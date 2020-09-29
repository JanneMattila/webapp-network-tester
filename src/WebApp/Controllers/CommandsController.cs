using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
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
            var output = new StringBuilder();
            var continueCommands = true;

            while (continueCommands)
            { 
                continueCommands = false;
                var input = request.Commands.FirstOrDefault();
                if (string.IsNullOrEmpty(input))
                {
                    break;
                }

                output.AppendLine($"-> Start: {input}");
                var commands = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (commands.Length > 0)
                {
                    var childRequest = new CommandRequest();
                    childRequest.Commands.AddRange(request.Commands.Skip(1));

                    var parameters = commands.Skip(1).ToArray();
                    if (commands.Length > 2 &&
                        (commands[0] != "HTTP" ||
                        commands[1] != "POST"))
                    {
                        continueCommands = true;
                    }

                    try
                    {
                        var childOutput = commands[0] switch
                        {
                            "HTTP" => await ExecuteHttpAsync(parameters, childRequest),
                            "BLOB" => await ExecuteBlobAsync(parameters),
                            "REDIS" => await ExecuteRedisAsync(parameters),
                            _ => string.Empty
                        };
                        if (!string.IsNullOrEmpty(input))
                        {
                            output.Append(childOutput);
                        }
                    }
                    catch (Exception ex)
                    {
                        output.AppendLine(ex.ToString());
                    }
                }
                output.AppendLine($"<- End: {input}");

                request.Commands = request.Commands.Skip(1).ToList();
            }
            return Content(output.ToString());
        }
        private async Task<string> ExecuteHttpAsync(string[] parameters, CommandRequest request)
        {
            using var client = new HttpClient();
            HttpResponseMessage response;
            if (parameters[0] == "GET")
            {
                response = await client.GetAsync(parameters[1]);
            }
            else
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
                response = await client.PostAsync(parameters[1], content);
            }
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return $"{response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}";
        }

        private async Task<string> ExecuteBlobAsync(string[] parameters)
        {
            var param = parameters.Reverse().ToArray();
            var blobServiceClient = new BlobServiceClient(param[0]);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(param[1]);
            await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            var blobClient = blobContainerClient.GetBlobClient(param[2]);

            if (parameters[0] == "GET")
            {
                var content = await blobClient.DownloadAsync();
                using var reader = new StreamReader(content.Value.Content);
                return await reader.ReadToEndAsync();
            }
            else
            {
                await blobClient.DeleteIfExistsAsync();
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(parameters[1]));
                var blobResponse = await blobClient.UploadAsync(stream);
                return $"Wrote {blobResponse.Value.ETag}{Environment.NewLine}";
            }
        }

        private async Task<string> ExecuteRedisAsync(string[] parameters)
        {
            var param = parameters.Reverse().ToArray();
            using var redis = ConnectionMultiplexer.Connect(param[0]);
            var db = redis.GetDatabase();

            if (parameters[0] == "GET")
            {
                var value = await db.StringGetAsync(parameters[1]);
                return $"GET: {value}{Environment.NewLine}";
            }
            else
            {
                await db.StringSetAsync(parameters[2], parameters[1]);
                return $"SET: {parameters[2]}={parameters[1]}{Environment.NewLine}";
            }
        }
    }
}
