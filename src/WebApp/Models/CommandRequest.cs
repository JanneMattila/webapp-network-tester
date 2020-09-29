using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebApp.Models
{
    public class CommandRequest
    {
        [JsonPropertyName("commands")]
        public List<string> Commands { get; set; }

        public CommandRequest()
        {
            Commands = new List<string>();
        }
    }
}
