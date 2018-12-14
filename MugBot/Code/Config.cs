using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MugBot.Code
{
    public class Config
    {
        public MattermostConfig MmConfig { get; set; }
        public string Secret { get; set; }
        public string CelebrationEmoji { get; set; }
        public string CustomString { get; set; }

        public List<string> IgnoredUsers { get; set; }

        public void Save(string path)
        {
            // serialize JSON directly to a file
            using (var file = File.CreateText(path))
            {
                var serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(file, this);
            }
        }
    }

    public class IgnoredUser
    {
        public string UserName { get; set; }
        public string PullRequestUrl { get; set; }
    }

    public class MattermostConfig
    {
        public string WebhookUrl { get; set; }
        public string Channel { get; set; }
        public string Username { get; set; }
        public string IconUrl { get; set; }
    }
}
