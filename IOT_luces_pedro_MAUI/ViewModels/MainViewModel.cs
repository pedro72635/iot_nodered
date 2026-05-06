using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Input;
using IOT_luces_pedro_MAUI.Models;
using IOT_luces_pedro_MAUI.Services;
using Microsoft.Maui.Controls;

namespace IOT_luces_pedro_MAUI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly McpClientService _mcpService;
    private readonly LlmService _llmService;

    public ObservableCollection<ChatMessage> Messages { get; set; } = new();
    
    private JsonArray _messagesHistory = new();
    private JsonArray? _mcpTools;

    private string _userInput = string.Empty;
    public string UserInput
    {
        get => _userInput;
        set { _userInput = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public ICommand SendCommand { get; }

    public MainViewModel()
    {
        _mcpService = new McpClientService();
        _llmService = new LlmService();
        SendCommand = new Command(async () => await OnSendAsync(), () => !IsBusy);

        Messages.Add(new ChatMessage { Role = "system", Content = "Iniciando sistema... conectando localmente al servidor MCP." });
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            await _mcpService.StartAsync();
            var toolsNode = await _mcpService.GetToolsAsync();
            if (toolsNode is JsonArray toolsArray)
            {
                _mcpTools = toolsArray;
            }

            _messagesHistory.Add(new JsonObject
            {
                ["role"] = "system",
                ["content"] = "Eres un asistente de IOT para controlar luces. Responde brevemente y utiliza las herramientas proporcionadas para encender o apagar las luces según lo solicite el usuario."
            });

            Messages.Add(new ChatMessage { Role = "system", Content = $"Herramientas MCP cargadas: {_mcpTools?.Count ?? 0}. Listo para recibir comandos." });
        }
        catch (System.Exception ex)
        {
            Messages.Add(new ChatMessage { Role = "system", Content = $"Error al iniciar: {ex.Message}" });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnSendAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput)) return;

        string text = UserInput;
        UserInput = string.Empty;

        Messages.Add(new ChatMessage { Role = "user", Content = text });
        _messagesHistory.Add(new JsonObject { ["role"] = "user", ["content"] = text });

        IsBusy = true;

        try
        {
            var llmResponse = await _llmService.SendChatAsync(_messagesHistory, _mcpTools);
            
            if (llmResponse != null)
            {
                // Es vital inyectar la repuesta original del asistente (que contiene el objeto "tool_calls")
                _messagesHistory.Add(JsonNode.Parse(llmResponse.ToJsonString())!);

                
                if (llmResponse["tool_calls"] is JsonArray toolCalls && toolCalls.Count > 0)
                {
                    var toolCall = toolCalls[0];
                    var functionNode = toolCall?["function"];
                    var toolCallId = toolCall?["id"]?.GetValue<string>() ?? "call_001";
                    
                    if (functionNode != null)
                    {
                        string toolName = functionNode["name"]?.GetValue<string>() ?? "";
                        string argsStr = functionNode["arguments"]?.GetValue<string>() ?? "{}";
                        
                        var args = System.Text.Json.JsonSerializer.Deserialize<JsonObject>(argsStr) ?? new JsonObject();

                        Messages.Add(new ChatMessage { Role = "system", Content = $"Ejecutando herramienta: {toolName}..." });

                        // Call MCP Server
                        string toolResult = await _mcpService.CallToolAsync(toolName, args);

                        Messages.Add(new ChatMessage { Role = "system", Content = $"Resultado MCP: {toolResult}" });

                        _messagesHistory.Add(new JsonObject
                        {
                            ["role"] = "tool",
                            ["tool_call_id"] = toolCallId,
                            ["name"] = toolName,
                            ["content"] = toolResult
                        });

                    }
                }
                else
                {
                    string content = llmResponse["content"]?.GetValue<string>() ?? "Sin respuesta textual.";
                    Messages.Add(new ChatMessage { Role = "assistant", Content = content });
                   
                }
            }
            else
            {
                Messages.Add(new ChatMessage { Role = "system", Content = "Error al recibir respuesta del LLM." });
            }
        }
        catch (System.Exception ex)
        {
            Messages.Add(new ChatMessage { Role = "system", Content = $"Error en la llamada: {ex.Message}" });
        }
        finally
        {
            IsBusy = false;
            ((Command)SendCommand).ChangeCanExecute();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
