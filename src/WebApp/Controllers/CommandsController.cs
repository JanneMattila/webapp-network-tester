using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DnsClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebApp.Controllers
{
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
        public async Task<ContentResult> Post()
        {
            using var reader = new StreamReader(this.Request.Body);
            var requestContent = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(requestContent))
            {
                return Content("-");
            }

            var output = new StringBuilder();
            var continueCommands = true;
            var requestCommands = requestContent
                .Replace("\r", "")
                .Split("\n", StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            while (continueCommands)
            { 
                continueCommands = false;
                var input = requestCommands.FirstOrDefault();
                if (string.IsNullOrEmpty(input))
                {
                    break;
                }

                output.AppendLine($"-> Start: {input}");
                var commands = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (commands.Length > 0)
                {
                    var childRequest = string.Join(Environment.NewLine, requestCommands.Skip(1));

                    var parameters = commands.Skip(1).ToArray();
                    if (commands.Length >= 2 &&
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
                            "IPLOOKUP" => await ExecuteIpLookUpAsync(parameters),
                            "NSLOOKUP" => await ExecuteNsLookUpAsync(parameters),
                            _ => string.Empty
                        };
                        if (!string.IsNullOrEmpty(input))
                        {
                            output.AppendLine(childOutput);
                        }
                    }
                    catch (Exception ex)
                    {
                        output.AppendLine(ex.ToString());
                    }
                }
                output.AppendLine($"<- End: {input}");

                requestCommands = requestCommands.Skip(1).ToList();
            }
            return Content(output.ToString());
        }
        private async Task<string> ExecuteHttpAsync(string[] parameters, string request)
        {
            using var client = new HttpClient();
            HttpResponseMessage response;
            if (parameters[0] == "GET")
            {
                response = await client.GetAsync(parameters[1]);
            }
            else
            {
                var content = new StringContent(request);
                response = await client.PostAsync(parameters[1], content);
            }
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return $"{response.StatusCode} {response.ReasonPhrase}";
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
                return $"Wrote {blobResponse.Value.ETag}";
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
                return $"GET: {value}";
            }
            else
            {
                await db.StringSetAsync(parameters[2], parameters[1]);
                return $"SET: {parameters[2]}={parameters[1]}";
            }
        }

        private async Task<string> ExecuteIpLookUpAsync(string[] parameters)
        {
            var output = new StringBuilder();
            var addresses = await Dns.GetHostAddressesAsync(parameters[0]);
            foreach (var address in addresses)
            {
                output.AppendLine($"IP: {address}");
            }
            return output.ToString();
        }

        private async Task<string> ExecuteNsLookUpAsync(string[] parameters)
        {
            var output = new StringBuilder();
            LookupClientOptions options;

            if (parameters.Length > 1)
            {
                options = new LookupClientOptions(IPAddress.Parse(parameters[1]));
            }
            else
            {
                options = new LookupClientOptions();
            }

            options.UseCache = false;
            options.Recursion = true;
            options.EnableAuditTrail = true;

            var lookup = new LookupClient(options);

            var query = await lookup.QueryAsync(parameters[0], QueryType.ANY);
            output.AppendLine($"NS: {query.NameServer.Address}");
            output.AppendLine($"AUDIT: {query.AuditTrail}");
            foreach (var address in query.Answers)
            {
                output.AppendLine($"RECORD: {address}");
            }
            return output.ToString();
        }
    }
}
