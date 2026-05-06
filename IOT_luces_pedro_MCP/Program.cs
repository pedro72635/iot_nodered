using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

// MCP C# Stdio Server adaptado para el endpoint unificado /luz
class Program
{
    private static readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:1880") };
    
    static async Task Main(string[] args)
    {
        Console.Error.WriteLine("MCP Server started. Waiting for input on stdin...");
        
        using var reader = new StreamReader(Console.OpenStandardInput());
        string? line;
        
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            try
            {
                var request = JsonNode.Parse(line);
                if (request == null) continue;
                
                await HandleRequest(request);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing line: {ex.Message}");
            }
        }
    }

    static async Task HandleRequest(JsonNode request)
    {
        var id = request["id"]?.GetValue<object>()?.ToString();
        var method = request["method"]?.GetValue<string>();
        
        if (method == "initialize")
        {
            var response = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = new JsonObject
                {
                    ["protocolVersion"] = "2024-11-05",
                    ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
                    ["serverInfo"] = new JsonObject
                    {
                        ["name"] = "IOTLucesMCP",
                        ["version"] = "2.0.0"
                    }
                }
            };
            SendResponse(response.ToJsonString());
        }
        else if (method == "tools/list")
        {
            var response = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = new JsonObject
                {
                    ["tools"] = new JsonArray
                    {
                        CreateToolDefinition("encender_roja", "Enciende la luz roja enviando mensaje 'encender_roja'."),
                        CreateToolDefinition("encender_verde", "Enciende la luz verde enviando mensaje 'encender_verde'."),
                        CreateToolDefinition("encender_amarilla", "Enciende la luz amarilla enviando mensaje 'encender_amarilla'."),
                        CreateToolDefinition("reproducir_musica", "Reproduce una breve melodía de 8-bits en el dispositivo IoT enviando el mensaje 'reproducir_musica'.")
                    }
                }
            };
            SendResponse(response.ToJsonString());
        }
        else if (method == "tools/call")
        {
            var toolName = request["params"]?["name"]?.GetValue<string>();
            string resultText = "Desconocido";

            // Verificar si el toolName es uno de los permitidos por Node-RED
            if (toolName == "encender_roja" || toolName == "encender_verde" || toolName == "encender_amarilla" || toolName == "reproducir_musica")
            {
                try
                {
                    var payload = new JsonObject { ["mensaje"] = toolName };
                    var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
                    
                    var httpResponse = await httpClient.PostAsync("/luz", content);
                    var responseBody = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        resultText = $"Comando {toolName} enviado a /luz. Node-RED respondió: {responseBody}";
                    }
                    else
                    {
                        resultText = $"Error HTTP {httpResponse.StatusCode}. Node-RED dice: {responseBody}";
                    }
                }
                catch (Exception ex)
                {
                    resultText = $"Error de conexión con Node-RED. Detalles: {ex.Message}";
                }
            }
            else
            {
                resultText = $"Herramienta '{toolName}' no reconocida o fuera de catálogo.";
            }

            var response = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = new JsonObject
                {
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = resultText
                        }
                    }
                }
            };
            SendResponse(response.ToJsonString());
        }
    }

    static void SendResponse(string json)
    {
        Console.WriteLine(json);
        Console.Out.Flush();
    }
    
    static JsonObject CreateToolDefinition(string name, string description)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["description"] = description,
            ["inputSchema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject()
            }
        };
    }
}
