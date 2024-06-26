using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DnsClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace WebApp.Controllers;

/// <summary>
/// Endpoint for executing remote commands
/// </summary>
[Produces("plain/text")]
[Route("api/[controller]")]
[ApiController]
public class CommandsController(ILogger<CommandsController> logger) : ControllerBase
{
    private readonly ILogger<CommandsController> _logger = logger;
    private readonly HttpClient _client = new();

    /// <summary>
    /// Execute network testing scenario.
    /// </summary>
    /// <remarks>
    /// Example find request:
    ///
    ///     POST /api/commands
    ///     IPLOOKUP office.com
    ///
    /// </remarks>
    /// <param name="body">Network test operation request</param>
    /// <returns>Network test operation results</returns>
    /// <response code="200">Returns network test operation results</response>
    /// <response code="500">If errors occur</response>
    [Consumes("text/plain", "application/x-www-form-urlencoded", "application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "plain/text")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost]
    public async Task<ContentResult> Post([FromBody] string body)
    {
        _logger.LogInformation("Processing request with body '{Body}'", body);

        var time = Stopwatch.StartNew();
        if (string.IsNullOrEmpty(body))
        {
            return Content("-");
        }

        var output = new StringBuilder();
        var continueCommands = true;
        var requestCommands = ParseCommand(body);

        while (continueCommands)
        {
            continueCommands = false;
            var input = requestCommands.FirstOrDefault();
            if (string.IsNullOrEmpty(input))
            {
                break;
            }

            if (output.Length > 0)
            {
                output.AppendLine();
            }

            output.AppendLine($"-> Start: {input}");
            var commands = ParseSingleCommand(input);
            if (commands.Count > 0)
            {
                var childRequest = string.Join(Environment.NewLine, requestCommands.Skip(1));

                var parameters = commands.Skip(1).ToArray();
                if (commands.Count >= 2 &&
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
                        "TCP" => await ExecuteTcpAsync(parameters),
                        "BLOB" => await ExecuteBlobAsync(parameters),
                        "FILE" => await ExecuteFileAsync(parameters),
                        "REDIS" => await ExecuteRedisAsync(parameters),
                        "SQL" => await ExecuteSQLAsync(parameters),
                        "IPLOOKUP" => await ExecuteIpLookUpAsync(parameters),
                        "NSLOOKUP" => await ExecuteNsLookUpAsync(parameters),
                        "INFO" => ExecuteInfo(parameters),
                        "HEADER" => ExecuteHeader(parameters),
                        "CONNECTION" => ExecuteConnection(parameters),
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
            output.Append($"<- End: {input} {time.Elapsed.TotalMilliseconds:F}ms");

            requestCommands = requestCommands.Skip(1).ToList();
        }
        return Content(output.ToString());
    }

    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "<Pending>")]
    private static List<string> ParseCommand(string requestContent)
    {
        if (requestContent.StartsWith("\"") &&
            requestContent.EndsWith("\""))
        {
            try
            {
                var content = JsonSerializer.Deserialize<string>(requestContent);
                requestContent = content;
            }
            catch (Exception)
            {
            }
        }

        return requestContent.Replace("\r", "")
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static List<string> ParseSingleCommand(string input)
    {
        const char separator = '\"';
        const char space = ' ';
        var parameters = new List<string>();
        var parameter = string.Empty;
        var insideString = false;
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            switch (c)
            {
                case separator:
                    insideString = !insideString;
                    break;
                case space:
                    if (insideString)
                    {
                        parameter += c;
                    }
                    else
                    {
                        parameters.Add(parameter);
                        parameter = string.Empty;
                    }
                    break;
                default:
                    parameter += c;
                    break;
            }
        }

        if (!string.IsNullOrEmpty(parameter))
        {
            parameters.Add(parameter);
        }
        return parameters;
    }

    private async Task<string> ExecuteHttpAsync(string[] parameters, string request)
    {
        // Process headers
        if (parameters.Length > 2)
        {
            var headers = parameters[2].Split('|', StringSplitOptions.RemoveEmptyEntries);
            foreach (var header in headers)
            {
                var data = header.Split('=');
                _client.DefaultRequestHeaders.Add(data[0], data[1]);
            }
        }

        HttpResponseMessage response;
        if (parameters[0] == "GET")
        {
            response = await _client.GetAsync(parameters[1]);
        }
        else
        {
            var content = new StringContent(request);
            response = await _client.PostAsync(parameters[1], content);
        }
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            var content = string.Empty;
            try
            {
                content = await response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                // Getting error content is optional.
            }
            return $"{response.StatusCode} {response.ReasonPhrase} {content}";
        }
    }

    private static async Task<string> ExecuteTcpAsync(string[] parameters)
    {
        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(parameters[0], Convert.ToInt32(parameters[1]));
            client.Close();
            return "OK";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static async Task<string> ExecuteBlobAsync(string[] parameters)
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

    private static async Task<string> ExecuteFileAsync(string[] parameters)
    {
        if (parameters[0] == "LIST")
        {
            if (System.IO.Directory.Exists(parameters[1]))
            {
                return string.Join(Environment.NewLine, System.IO.Directory.EnumerateFileSystemEntries(parameters[1]));
            }
            return $"LIST: Directory \"{parameters[1]}\" not found";
        }
        else if (parameters[0] == "READ")
        {
            if (System.IO.File.Exists(parameters[1]))
            {
                return await System.IO.File.ReadAllTextAsync(parameters[1]);
            }
            return $"READ: File \"{parameters[1]}\" not found";
        }
        else
        {
            await System.IO.File.WriteAllTextAsync(parameters[1], parameters[2]);
            return $"WRITE: {parameters[1]}={parameters[2]}";
        }
    }

    private static async Task<string> ExecuteRedisAsync(string[] parameters)
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

    private static async Task<string> ExecuteSQLAsync(string[] parameters)
    {
        var output = new StringBuilder();
        var param = parameters.Reverse().ToArray();
        using var connection = new SqlConnection(param[0]);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = param[1];
        using var reader = command.ExecuteReader();
        var columns = reader.GetColumnSchema();
        var columnNames = columns.Select(c => c.ColumnName);
        output.AppendLine(string.Join(";", columnNames));

        if (reader.HasRows)
        {
            while (reader.Read())
            {
                var values = columnNames.Select(c => reader.GetProviderSpecificValue(c));
                output.AppendLine(string.Join(";", values));
            }
        }
        else
        {
            output.AppendLine("No rows found.");
        }
        return output.ToString();
    }

    private static async Task<string> ExecuteIpLookUpAsync(string[] parameters)
    {
        var output = new StringBuilder();
        var addresses = await Dns.GetHostAddressesAsync(parameters[0]);
        foreach (var address in addresses)
        {
            output.AppendLine($"IP: {address}");
        }
        return output.ToString();
    }

    private static async Task<string> ExecuteNsLookUpAsync(string[] parameters)
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

    private static string ExecuteInfo(string[] parameters)
    {
        if (parameters[0] == "HOSTNAME")
        {
            return $"HOSTNAME: {Environment.MachineName}";
        }
        else if (parameters[0] == "NETWORK")
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var output = new StringBuilder();
            foreach (var networkInterface in networkInterfaces)
            {
                output.AppendLine($"Network interface: {networkInterface.Name}");
                output.AppendLine($"Id: {networkInterface.Id}");
                output.AppendLine($"Description: {networkInterface.Description}");
                var ipProps = networkInterface.GetIPProperties();

                output.AppendLine($"Statistics:");

                var stats = networkInterface.GetIPStatistics();
                output.AppendLine($"- Bytes received: {stats.BytesReceived}");
                output.AppendLine($"- Bytes sent: {stats.BytesSent}");
                output.AppendLine($"- Incoming packets discarded: {stats.IncomingPacketsDiscarded}");
                output.AppendLine($"- Incoming packets with errors: {stats.IncomingPacketsWithErrors}");
                if (OperatingSystem.IsWindows())
                {
                    output.AppendLine($"- Incoming unknown protocol packets: {stats.IncomingUnknownProtocolPackets}");
                    output.AppendLine($"- Non unicast packets sent: {stats.NonUnicastPacketsSent}");
                    output.AppendLine($"- Outgoing packets discarded: {stats.OutgoingPacketsDiscarded}");
                }
                output.AppendLine($"- Non unicast packets received: {stats.NonUnicastPacketsReceived}");
                output.AppendLine($"- Outgoing packets with errors: {stats.OutgoingPacketsWithErrors}");
                output.AppendLine($"- Output queue length: {stats.OutputQueueLength}");
                output.AppendLine($"- Unicast packets received: {stats.UnicastPacketsReceived}");
                output.AppendLine($"- Unicast packets sent: {stats.UnicastPacketsSent}");

                output.AppendLine($"Status: {networkInterface.OperationalStatus}");
                output.AppendLine($"Type: {networkInterface.NetworkInterfaceType}");
                output.AppendLine($"Speed: {networkInterface.Speed}");

                output.AppendLine($"Gateway addresses:");
                foreach (var gateway in ipProps.GatewayAddresses)
                {
                    output.AppendLine($"- Gateway address: {gateway.Address}");
                }

                output.AppendLine($"DNS addresses:");
                foreach (var dns in ipProps.DnsAddresses)
                {
                    output.AppendLine($"- DNS address: {dns}");
                }
            }
            return output.ToString();
        }
        else
        {
            if (parameters.Length > 1)
            {
                return $"ENV: {parameters[1]}: {Environment.GetEnvironmentVariable(parameters[1])}";
            }
            else
            {
                var output = new StringBuilder();
                foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
                {
                    output.AppendLine($"ENV: {env.Key}: {env.Value}");
                }
                return output.ToString();
            }
        }
    }

    private string ExecuteHeader(string[] parameters)
    {
        if (parameters.Length > 1)
        {
            return $"HEADER: {parameters[1]}: {Request.Headers.FirstOrDefault(h => h.Key == parameters[1])}";
        }
        else
        {
            var output = new StringBuilder();
            foreach (var header in Request.Headers)
            {
                output.AppendLine($"HEADER: {header.Key}: {header.Value}");
            }
            return output.ToString();
        }
    }

    private string ExecuteConnection(string[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] == "IP")
        {
            return $"IP: {Request.HttpContext.Connection.RemoteIpAddress}";
        }
        return $"CONNECTION: {Request.HttpContext.Connection.RemoteIpAddress}:{Request.HttpContext.Connection.RemotePort}";
    }
}
