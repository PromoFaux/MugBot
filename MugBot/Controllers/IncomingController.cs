using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using GithubWebhook.Events;
using GithubWebhook;
using Matterhook.NET.MatterhookClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Config = MugBot.Code.Config;
using System.Linq;
using System.Text.RegularExpressions;

namespace MugBot.Controllers
{
    [Route("[Controller]")]
    public class IncomingController : Controller
    {
        private static Config _config;
        private static MatterhookClient _matterhook;

        public IncomingController(IOptions<Config> config)
        {
            try
            {
                _config = config.Value;
                _config.ConvertIgnoredUsers();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        [HttpPost("")]
        public async Task<IActionResult> Receive()
        {
            _matterhook = new MatterhookClient(_config.MmConfig.WebhookUrl);
            try
            {
                GhWebhook hook;
                try
                {
                    hook = new GhWebhook(Request, _config.Secret); //Signature validation has been moved to GithubWebhookLibrary
                }
                catch (Exception e)
                {
                    return StatusCode(400, e.Message);
                }

                var message = new MattermostMessage
                {
                    Channel = _config.MmConfig.Channel,
                    Username = _config.MmConfig.Username,
                    IconUrl = _config.MmConfig.IconUrl != null ? _config.MmConfig.IconUrl : null
                };

                switch (hook.Event)
                {
                    case PullRequestEvent.EventString:
                        {
                            var pr = (PullRequestEvent)hook.PayloadObject;

                            if (pr.Action == "closed")
                            {
                                if (pr.PullRequest.Merged != null && (bool)pr.PullRequest.Merged)
                                {
                                    var user = pr.PullRequest.User.Login;

                                    if (_config.IgnoredUsers_Detailed == null)
                                    {
                                        _config.IgnoredUsers_Detailed = new List<Code.IgnoredUser>();
                                    }

                                    if (_config.IgnoredUsers_Detailed.Exists(x => x.UserName == user))
                                    {
                                        return StatusCode(200, $"{user} is already in my list!");
                                    }

                                    message.Text += "#users-first-contribution\n";

                                    var usrMd = $"[{user}]({pr.PullRequest.User.HtmlUrl})";

                                    if (_config.CelebrationEmoji != null)
                                        message.Text += $"{_config.CelebrationEmoji} ";

                                    message.Text += $"A User has had a pull request merged for the first time!";

                                    if (_config.CelebrationEmoji != null)
                                        message.Text += $" {_config.CelebrationEmoji}";

                                    message.Text += $"\n\nUser: {usrMd}\nPull: {pr.PullRequest.HtmlUrl}";

                                    if (_config.CustomString != null)
                                        message.Text += $"\n\n {_config.CustomString}";

                                    var response = await _matterhook.PostAsync(message);

                                    if (response == null || response.StatusCode != HttpStatusCode.OK)
                                        return StatusCode(500, response != null
                                            ? $"Unable to post to Mattermost: {response.StatusCode}"
                                            : "Unable to post to Mattermost");

                                    _config.IgnoredUsers_Detailed.Add(new Code.IgnoredUser { UserName = user,
                                                                                             PullRequestUrl = pr.PullRequest.HtmlUrl,
                                                                                             ContributionDate = $"{pr.PullRequest.ClosedAt:dd MMMM yyyy HH:mm:ss}"
                                                                                           });
                                    _config.Save("/config/config.json");

                                    return StatusCode(200, "Succesfully posted to Mattermost");
                                }
                            }
                            else
                            {
                                return StatusCode(200, $"{pr.Action} actions ignored by this bot");
                            }

                            break;
                        }
                    case PingEvent.EventString:
                        return StatusCode(200, "Pong!");
                    default:
                        return StatusCode(200, $"{hook.Event} is not a valid event for this bot!");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return StatusCode(200, "Ignored"); //This is where it's falling out
        }

        [Route("ContributorList")]
        public ActionResult GetContributorList(SlashCommand incoming)
        {
            if (incoming.token != _config.SlashToken || string.IsNullOrEmpty(_config.SlashToken))
            {
                return Json(new
                {
                    icon_url = _config.MmConfig.IconUrl,
                    text = "slashToken is not set up!"
                });
            }
            else
            {
                var rtnTxt = "";

                if (_config.IgnoredUsers_Detailed.Count > 0)
                {
                    rtnTxt = "| User | Url | Date |\n|:---|:---|:---|\n";
                    foreach (var user in _config.IgnoredUsers_Detailed)
                    {
                        rtnTxt += $"|{user.UserName}|{user.PullRequestUrl}|{user.ContributionDate}\n";
                    }
                }
                else
                {
                    rtnTxt = "No contributions counted yet!";
                }

                return Json(new
                {
                    response_type = "in_channel",
                    icon_url = _config.MmConfig.IconUrl,
                    text = rtnTxt
                });
            }
        }

        public class SlashCommand
        {
            public string channel_id { get; set; }
            public string channel_name { get; set; }
            public string command { get; set; }
            public string response_url { get; set; }
            public string team_domain { get; set; }
            public string team_id { get; set; }
            public string text { get; set; }
            public string token { get; set; }
            public string user_id { get; set; }
            public string user_name { get; set; }
        }

    }


}