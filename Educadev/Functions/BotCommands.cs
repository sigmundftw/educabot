using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Educadev.Helpers;
using Educadev.Models.Slack.Dialogs;
using Educadev.Models.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace Educadev.Functions
{
    public static class BotCommands
    {
        [FunctionName("SlackCommandPropose")]
        public static async Task<IActionResult> OnPropose(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack/commands/propose")] HttpRequest req,
            ILogger log)
        {
            var body = await SlackHelper.ReadSlackRequest(req);
            var parameters = SlackHelper.ParseBody(body);

            var dialogRequest = new OpenDialogRequest {
                TriggerId = parameters["trigger_id"],
                Dialog = GetProposeDialog(defaultName: parameters["text"])
            };

            await SlackHelper.SlackPost("dialog.open", parameters["team_id"], dialogRequest);

            return Utils.Ok();
        }

        [FunctionName("SlackCommandList")]
        public static async Task<IActionResult> OnList(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack/commands/list")] HttpRequest req,
            [Table("proposals")] CloudTable proposalsTable,
            ILogger log)
        {
            var body = await SlackHelper.ReadSlackRequest(req);
            var parameters = SlackHelper.ParseBody(body);

            var team = parameters["team_id"];
            var channel = parameters["channel_id"];

            var allProposals = await proposalsTable.GetAllByPartition<Proposal>(SlackHelper.GetPartitionKey(team, channel));

            var message = MessageHelpers.GetListMessage(allProposals, channel);

            return Utils.Ok(message);
        }

        [FunctionName("SlackCommandPlan")]
        public static async Task<IActionResult> OnPlan(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "slack/commands/plan")] HttpRequest req,
            [Table("proposals")] CloudTable proposalsTable,
            ILogger log)
        {
            var body = await SlackHelper.ReadSlackRequest(req);
            var parameters = SlackHelper.ParseBody(body);
            
            var allProposals = await proposalsTable.GetAllByPartition<Proposal>(SlackHelper.GetPartitionKey(parameters["team_id"], parameters["channel_id"]));

            var dialogRequest = new OpenDialogRequest {
                TriggerId = parameters["trigger_id"],
                Dialog = GetPlanDialog(allProposals)
            };

            await SlackHelper.SlackPost("dialog.open", parameters["team_id"], dialogRequest);

            return Utils.Ok();
        }

        private static Dialog GetProposeDialog(string defaultName)
        {
            return new Dialog {
                CallbackId = "propose",
                Title = "Proposer un vid�o",
                SubmitLabel = "Proposer",
                Elements = new List<DialogElement> {
                    new TextDialogElement("name", "Nom du vid�o") {
                        MaxLength = 40,
                        Placeholder = "How to use a computer",
                        DefaultValue = defaultName
                    },
                    new TextDialogElement("url", "URL vers la vid�o") {
                        Subtype = "url",
                        Placeholder = "http://example.com/my-awesome-video",
                        Hint = @"Si le vid�o est sur le r�seau, inscrivez le chemin vers le fichier partag�, d�butant par \\"
                    },
                    new TextareaDialogElement("notes", "Notes") {
                        Optional = true
                    }
                }
            };
        }

        private static Dialog GetPlanDialog(IList<Proposal> allProposals)
        {
            var dialog = new Dialog {
                CallbackId = "plan",
                Title = "Planifier un Lunch&Watch",
                SubmitLabel = "Planifier",
                Elements = new List<DialogElement> {
                    new TextDialogElement("date", "Date") {
                        Hint = "Au format AAAA-MM-JJ"
                    },
                    new SelectDialogElement("owner", "Responsable") {
                        Optional = true,
                        DataSource = "users",
                        Hint = "Si non choisi, le bot va demander un volontaire."
                    }
                }
            };

            if (allProposals.Any())
            {
                dialog.Elements.Add(new SelectDialogElement("video", "Vid�o") {
                    Optional = true,
                    Options = allProposals
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