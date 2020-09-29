using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebApp.Models
{
    public class CommandRequest
    {
        [JsonPropertyName("commands")]
        public List<string> Command { get; set; }

        public CommandRequest()
        {
            Command = new List<string>();
        }
    }
}
