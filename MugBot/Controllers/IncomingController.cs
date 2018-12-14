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

                                    if (_config.IgnoredUsers == null) _config.IgnoredUsers = new List<string>();

                                    if (_config.IgnoredUsers.Contains(user))
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

                                    _config.IgnoredUsers.Add(user);
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
                        return StatusCode(501, $"{hook.Event} is not a valid event for this bot!");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return StatusCode(200, "Ignored"); //This is where it's falling out
        }
    }
}