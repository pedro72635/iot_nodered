using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IOT_luces_pedro_MAUI.Services;

public class McpClientService
{
    private Process? _mcpProcess;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private int _messageId = 1;

    public async Task StartAsync()
    {
        var exePath = GetMcpServerPath();
        var tcs = new TaskCompletionSource<bool>();
        
        _mcpProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project \"" + exePath + "\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        _mcpProcess.Start();
        _stdin = _mcpProcess.StandardInput;
        _stdout = _mcpProcess.StandardOutput;
        
        // Initialize MCP
        var result = await SendRequestAsync("initialize", new JsonObject());
    }

    private string GetMcpServerPath()
    {
       
        string currentDir = AppDomain.CurrentDomain.BaseDirectory;
        
        while(!currentDir.EndsWith("NUEVO") && !currentDir.EndsWith("NUEVO\\") && Directory.GetParent(currentDir) != null)
        {
            currentDir = Directory.GetParent(currentDir)!.FullName;
            if(Directory.Exists(Path.Combine(currentDir, "IOT_luces_pedro_MCP"))) {
                return Path.Combine(currentDir, "IOT_luces_pedro_MCP", "IOT_luces_pedro_MCP.csproj");
            }
        }
        return Path.Combine(currentDir, "IOT_luces_pedro_MCP", "IOT_luces_pedro_MCP.csproj");
    }

    public async Task<JsonNode?> GetToolsAsync()
    {
        var response = await SendRequestAsync("tools/list", new JsonObject());
        return response?["result"]?["tools"];
    }

    public async Task<string> CallToolAsync(string toolName, JsonObject arguments)
    {
        var response = await SendRequestAsync("tools/call", new JsonObject
        {
            ["name"] = toolName,
            ["arguments"] = arguments
        });

        return response?["result"]?["content"]?[0]?["text"]?.GetValue<string>() ?? "Error";
    }

    private async Task<JsonNode?> SendRequestAsync(string method, JsonObject parameters)
    {
        if (_stdin == null || _stdout == null) throw new InvalidOperationException("MCP no iniciado.");

        int id = _messageId++;
        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id.ToString(),
            ["method"] = method,
            ["params"] = parameters
        };

        await _stdin.WriteLineAsync(request.ToJsonString());
        await _stdin.FlushAsync();

        string? responseStr = await _stdout.ReadLineAsync();
        if (string.IsNullOrEmpty(responseStr)) return null;

        return JsonNode.Parse(responseStr);
    }
}
