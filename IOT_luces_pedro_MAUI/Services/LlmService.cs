using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IOT_luces_pedro_MAUI.Services;

public class LlmService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl = "http://localhost:1234/v1/chat/completions";

    public LlmService()
    {
        _http = new HttpClient();
        _http.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<JsonNode?> SendChatAsync(JsonArray messages, JsonArray? tools)
    {
        var requestBody = new JsonObject
        {
            ["model"] = "local-model", 
            ["messages"] = JsonNode.Parse(messages.ToJsonString()), 
            ["temperature"] = 0.7
        };

        if (tools != null && tools.Count > 0)
        {
            var openAiTools = new JsonArray();
            foreach (var tool in tools)
            {
                
                var functionDef = new JsonObject
                {
                    ["name"] = tool["name"]?.GetValue<string>(),
                    ["description"] = tool["description"]?.GetValue<string>(),
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject()
                    }
                };

                openAiTools.Add(new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = functionDef
                });
            }
            requestBody["tools"] = openAiTools;
        }

        var content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync(_baseUrl, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseNodes = JsonNode.Parse(responseString);

            return responseNodes?["choices"]?[0]?["message"];
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error calling LLM: " + ex.Message);
            return null;
        }
    }
}
