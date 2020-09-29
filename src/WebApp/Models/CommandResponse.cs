using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebApp.Models
{
    public class CommandResponse
    {
        [JsonPropertyName("responses")]
        public List<string> Responses { get; set; }

        public CommandResponse()
        {
            Responses = new List<string>();
        }
    }
}
