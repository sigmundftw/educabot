﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Educadev.Models.Slack.Dialogs;
using Educadev.Models.Tables;
using Microsoft.Azure.WebJobs;

namespace Educadev.Helpers
{
    public static class DialogHelpers
    {
        public static Dialog GetProposeDialog(string defaultName)
        {
            return new Dialog {
                CallbackId = "propose",
                Title = "Proposer un vidéo",
                SubmitLabel = "Proposer",
                Elements = new List<DialogElement> {
                    new TextDialogElement("name", "Nom du vidéo") {
                        MaxLength = 40,
                        Placeholder = "How to use a computer",
                        DefaultValue = defaultName
                    },
                    new TextDialogElement("url", "URL vers la vidéo") {
                        Subtype = "url",
                        Placeholder = "http://example.com/my-awesome-video",
                        Hint = @"Si le vidéo est sur le réseau, inscrivez le chemin vers le fichier partagé, débutant par \\"
                    },
                    new TextareaDialogElement("notes", "Notes") {
                        Optional = true
                    }
                }
            };
        }

        public static async Task<Dialog> GetPlanDialog(IBinder binder, string partitionKey, string defaultDate = null)
        {
            var dialog = new Dialog {
                CallbackId = "plan",
                State = partitionKey,
                Title = "Planifier un Lunch&Watch",
                SubmitLabel = "Planifier",
                Elements = new List<DialogElement> {
                    new TextDialogElement("date", "Date") {
                        Hint = "Au format AAAA-MM-JJ",
                        DefaultValue = defaultDate
                    },
                    new SelectDialogElement("owner", "Responsable") {
                        Optional = true,
                        DataSource = "users",
                        Hint = "Si non choisi, le bot va demander un volontaire."
                    }
                }
            };

            var table = await binder.GetTable("proposals");
            var allProposals = await ProposalHelpers.GetActiveProposals(table, partitionKey);

            if (allProposals.Any())
            {
                dialog.Elements.Add(new SelectDialogElement("video", "Vidéo") {
                    Optional = true,
                    Options = allProposals
                        .Where(x => string.IsNullOrWhiteSpace(x.PlannedIn))
                        .Select(x => new SelectOption {
                            Label = x.Name,
                            Value = x.RowKey
                        }).ToArray(),
                    Hint = "Si non choisi, le bot va faire voter le channel."
                });
            }

            return dialog;
        }
    }
}