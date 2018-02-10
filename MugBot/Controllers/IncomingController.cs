using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Matterhook.NET.MatterhookClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MugBot.Code;
using MugBot.GithubSpec;

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
            var stuffToLog = new List<string>();
            _matterhook = new MatterhookClient(_config.MmConfig.WebhookUrl);
            try
            {
                string payloadText;

                //Generate GithubHook Object
                stuffToLog.Add($"Github Hook received: {DateTime.Now}");

                Request.Headers.TryGetValue("X-GitHub-Event", out var strEvent);
                Request.Headers.TryGetValue("X-Hub-Signature", out var signature);
                Request.Headers.TryGetValue("X-GitHub-Delivery", out var delivery);
                Request.Headers.TryGetValue("Content-type", out var content);

                stuffToLog.Add($"Hook Id: {delivery}");
                stuffToLog.Add($"X-Github-Event: {strEvent}");

                if (content != "application/json")
                {
                    const string error = "Invalid content type. Expected application/json";
                    stuffToLog.Add(error);
                    Util.LogList(stuffToLog);
                    return StatusCode(400, error);

                }

                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    payloadText = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                var calcSig = Util.CalculateSignature(payloadText, signature, _config.Secret, "sha1=");

                if (signature == calcSig)
                {
                    var githubHook = new GithubHook(strEvent, signature, delivery, payloadText);
                    HttpResponseMessage response = null;

                    var message = new MattermostMessage
                    {
                        Channel = _config.MmConfig.Channel,
                        Username = _config.MmConfig.Username,
                        IconUrl = _config.MmConfig.IconUrl != null ? new Uri(_config.MmConfig.IconUrl) : null
                    };

                    if (githubHook.Event == "pull_request")
                    {
                        var pr = (PullRequestEvent) githubHook.Payload;

                        if (pr.action == "closed")
                        {
                            if (pr.pull_request.merged)
                            {
                                var user = pr.pull_request.user.login;

                                if (_config.IgnoredUsers == null) _config.IgnoredUsers = new List<string>();

                                if (!_config.IgnoredUsers.Contains(user))
                                {
                                    var usrMd = $"[{user}]({pr.pull_request.user.html_url})";

                                    message.Text = $":celebrate: A User has had a pull request merged for the first time! :celebrate:\n\nUser: {usrMd}\nPull: {pr.pull_request.html_url}\n\n @{_config.NotifyUser} - Send them a mug please!";

                                    response = await _matterhook.PostAsync(message);

                                    if (response == null || response.StatusCode != HttpStatusCode.OK)
                                    {
                                        return StatusCode(500, response != null
                                            ? $"Unable to post to Mattermost: {response.StatusCode}"
                                            : "Unable to post to Mattermost");
                                    }

                                    _config.IgnoredUsers.Add(user);
                                    _config.Save("/config/config.json");
                                    
                                    return StatusCode(200, "Succesfully posted to Mattermost");
                                }
                                else
                                {
                                    return StatusCode(200, "This User has already contributed");
                                }
                                
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return StatusCode(200, "Ignored");
        }
    }
}


