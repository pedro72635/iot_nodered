# IOT Luces - Sistema de Control IoT con ESP32, Node-RED, MCP y .NET MAUI

Proyecto de Internet de las Cosas (IoT) que permite controlar luces LED y un buzzer desde una aplicación .NET MAUI con interfaz de chat inteligente, utilizando un servidor MCP (Model Context Protocol), Node-RED como intermediario y MQTT como protocolo de comunicación con el dispositivo ESP32.

## Arquitectura

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  .NET MAUI  │────▶│  MCP Server │────▶│  Node-RED   │────▶│   ESP32     │
│  (Chat UI)  │     │  (C#/.NET)  │     │  (HTTP/MQTT)│     │  (WiFi/MQTT)│
│             │◀────│             │     │             │◀────│             │
│  LLM Local  │     │  Stdio JSON │     │  /luz API   │     │  LEDs+OLED  │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
       │                                      │                     │
       └──────────────────────────────────────┼─────────────────────┘
                                              │
                                    ┌─────────────────┐
                                    │  test.mosquitto.org │
                                    │  (Broker MQTT)  │
                                    └─────────────────┘
```

## Componentes

### 1. Firmware ESP32 (`codigo_esp32_pedro_galera_fernandez/`)

Código Arduino para el ESP32 que:
- Se conecta a WiFi y al broker MQTT `test.mosquitto.org`
- Suscribe al topic `iot-luz-pedro-gf-2026`
- Controla 3 LEDs (rojo, verde, amarillo) y un buzzer
- Muestra el último comando en una pantalla OLED SSD1306

**Hardware requerido:**
- ESP32
- 3 LEDs (rojo, verde, amarillo)
- Buzzer activo
- Pantalla OLED SSD1306 (I2C)
- Resistencias y cables

**Pines:**
| Componente | Pin GPIO |
|---|---|
| LED Verde | GPIO 5 |
| LED Amarillo | GPIO 18 |
| LED Rojo | GPIO 19 |
| Buzzer | GPIO 4 |
| OLED SDA | GPIO 21 |
| OLED SCL | GPIO 22 |

**Librerías necesarias (Arduino IDE):**
- `WiFi.h` (incluida en ESP32 core)
- `PubSubClient`
- `Adafruit_GFX`
- `Adafruit_SSD1306`

### 2. Flow Node-RED (`flow_pedro_galera_iot.json`)

Flujo que expone un endpoint HTTP `POST /luz` y reenvía los comandos al broker MQTT.

**Instalación:**
1. Abrir Node-RED
2. Menu → Import → pegar el contenido de `flow_pedro_galera_iot.json`
3. Desplegar el flujo

### 3. MCP Server (`IOT_luces_pedro_MCP/`)

Servidor MCP (Model Context Protocol) en C#/.NET 9 que:
- Expone herramientas: `encender_roja`, `encender_verde`, `encender_amarilla`, `reproducir_musica`
- Se comunica con Node-RED vía HTTP (`localhost:1880/luz`)
- Se ejecuta como proceso hijo desde la app MAUI

**Requisitos:**
- .NET 9 SDK

**Ejecución standalone:**
```bash
cd IOT_luces_pedro_MCP
dotnet run
```

### 4. Aplicación MAUI (`IOT_luces_pedro_MAUI/`)

App multiplataforma con interfaz de chat que:
- Inicia el MCP Server como proceso hijo
- Consulta las herramientas disponibles al MCP
- Envía mensajes a un LLM local (compatible con API OpenAI)
- Ejecuta herramientas MCP cuando el LLM lo decide

**Requisitos:**
- .NET 9 SDK
- Visual Studio 2022 o VS Code con extensión C# Dev Kit
- LLM local corriendo en `localhost:1234` (compatible con LM Studio, Ollama, etc.)

**Ejecución:**
```bash
cd IOT_luces_pedro_MAUI
dotnet run
```

## Flujo de uso

1. **Configurar WiFi** en el firmware ESP32 (`ssid` y `password`)
2. **Flashear** el código en el ESP32
3. **Importar** el flow en Node-RED y desplegarlo
4. **Ejecutar** la app MAUI (esto inicia automáticamente el MCP Server)
5. **Escribir** comandos en el chat, ej: "Enciende la luz roja"

## Comandos disponibles

| Comando MQTT | Descripción |
|---|---|
| `encender_roja` | Enciende el LED rojo |
| `encender_verde` | Enciende el LED verde |
| `encender_amarilla` | Enciende el LED amarillo |
| `reproducir_musica` | Reproduce una melodía en el buzzer |

## Estructura del proyecto

```
├── codigo_esp32_pedro_galera_fernandez/
│   └── codigo_esp32_pedro_galera_fernandez.ino
├── IOT_luces_pedro_MCP/
│   ├── IOT_luces_pedro_MCP.csproj
│   └── Program.cs
├── IOT_luces_pedro_MAUI/
│   ├── IOT_luces_pedro_MAUI.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── AppShell.xaml / AppShell.xaml.cs
│   ├── MainPage.xaml / MainPage.xaml.cs
│   ├── MauiProgram.cs
│   ├── Converters/
│   │   └── RoleToColorConverter.cs
│   ├── Models/
│   │   └── ChatMessage.cs
│   ├── Services/
│   │   ├── McpClientService.cs
│   │   └── LlmService.cs
│   └── ViewModels/
│       └── MainViewModel.cs
├── flow_pedro_galera_iot.json
├── IOT_luces_pedro.sln
├── documentacion_tecnica_iot_pedro_galera.pdf
└── .gitignore
```

## Autor

**Pedro Galera Fernandez**
