using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nafanya.Models
{
    public class BotConfiguration
    {
        public string? BotToken { get; set; }
        public string? SourceChannelUsername { get; set; }
        public string? DestinationChannelUsername { get; set; }
        public string? TargetMessage { get; set; }
        public string? SqlConnectionString { get; set; }
        public string[]? AllowedIps { get; set; }
    }
}