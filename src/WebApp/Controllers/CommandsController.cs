using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Produces("application/json")]
    [ApiController]
    [Route("api/[controller]")]
    public class CommandsController : ControllerBase
    {
        private readonly ILogger<CommandsController> _logger;

        public CommandsController(ILogger<CommandsController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<ContentResult> Post(CommandRequest request)
        {
            var input = request.Commands.FirstOrDefault();
            var output = new StringBuilder();
            output.AppendLine($"-> Start: {input}");
            if (!string.IsNullOrEmpty(input))
            {
                var commands = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (commands.Length > 0)
                {
                    var childRequest = new CommandRequest();
                    childRequest.Commands.AddRange(request.Commands.Skip(1));

                    var parameters = commands.Skip(1).ToArray();
                    var childOutput = commands[0] switch
                    {
                        "GET" => await ExecuteGetAsync(parameters),
                        "POST" => await ExecutePostAsync(parameters, childRequest),
                        _ => string.Empty
                    };
                    if (!string.IsNullOrEmpty(input))
                    {
                        output.Append(childOutput);
                    }
                }
            }

            output.AppendLine($"<- End: {input}");
            return Content(output.ToString());
        }
        private async Task<string> ExecuteGetAsync(string[] parameters)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(parameters[0]);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return $"{response.StatusCode} {response.ReasonPhrase}";
        }

        private async Task<string> ExecutePostAsync(string[] parameters, CommandRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            using var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            var response = await client.PostAsync(parameters[0], content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return $"{response.StatusCode} {response.ReasonPhrase}";
        }
    }
}
