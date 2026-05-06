#include <WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

// Conexión WiFi
const char* ssid = "";
const char* password = "";

// Servidor MQTT
const char* mqtt_server = "test.mosquitto.org";
const char* mqtt_topic_sub = "iot-luz-pedro-gf-2026";

// Pines de Hardware (Ajustar según cableado físico ESP32)
const int PIN_LED_VERDE = 5;
const int PIN_LED_AMARILLA = 18;
const int PIN_LED_ROJA = 19;
const int PIN_BUZZER = 4;

// Pantalla OLED (I2C) - GPIO 21 (SDA), GPIO 22 (SCL) estandar
#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

WiFiClient espClient;
PubSubClient client(espClient);

void setup_wifi() {
  delay(10);
  Serial.println();
  Serial.print("Conectando a ");
  Serial.println(ssid);
  WiFi.begin(ssid, password);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("");
  Serial.println("WiFi conectado");
  Serial.println("IP address: ");
  Serial.println(WiFi.localIP());
}

void reproducir_musica() {

  
  int melody[] = {
    // --- Bucle 1 ---
    659, 698,                   
    784, 698, 659, 0, 523, 0, 440, 
    0, 523, 0, 440, 0, 523,     
    698, 659, 587, 494, 523, 0, 
    
    
    659, 698,                   
    784, 698, 659, 0, 523, 0, 440,
    0, 523, 0, 440, 0, 523,     
    698, 659, 587, 494, 523, 0 
  };

  int noteDurations[] = {
    // Bucle 1
    8, 8,              
    8, 8, 8, 8, 8, 8, 4, 
    8, 8, 8, 8, 4, 4,
    4, 8, 8, 8, 8, 4,
    
    // Bucle 2
    8, 8,
    8, 8, 8, 8, 8, 8, 4,
    8, 8, 8, 8, 4, 4,
    4, 8, 8, 8, 8, 2    
  };

  int numNotas = sizeof(melody) / sizeof(melody[0]);
  
  // TEMPO: 1200 lo hace bastante rápido y animado
  int tempo = 1200; 

  for (int thisNote = 0; thisNote < numNotas; thisNote++) {
    int duration = tempo / noteDurations[thisNote];
    
    // Si no es un 0 (silencio), toca la nota
    if (melody[thisNote] != 0) {
      tone(PIN_BUZZER, melody[thisNote], duration);
    }
    
    // Pausa un poco más corta (1.15) para que no pierda la velocidad
    int pauseBetweenNotes = duration * 1.15;
    delay(pauseBetweenNotes);
    
    noTone(PIN_BUZZER);
  }
}

void update_display(String color, String action) {
  display.clearDisplay();
  display.setCursor(0, 0);
  display.setTextSize(1);
  display.println("ULTIMO COMANDO:");
  
  display.setCursor(0, 20);
  display.setTextSize(2);
  display.print(action);
  display.print(" ");
  display.println(color);
  
  display.display();
}

void callback(char* topic, byte* payload, unsigned int length) {
  String messageTemp;
  for (int i = 0; i < length; i++) {
    messageTemp += (char)payload[i];
  }
  Serial.print("Mensaje recibido en topic: ");
  Serial.println(topic);
  Serial.print("Mensaje: ");
  Serial.println(messageTemp);

  if (messageTemp == "encender_roja") {
    digitalWrite(PIN_LED_ROJA, HIGH);
    digitalWrite(PIN_LED_VERDE, LOW);
    digitalWrite(PIN_LED_AMARILLA, LOW);
    update_display("ROJA", "ENCENDIDA");
  } else if (messageTemp == "encender_verde") {
    digitalWrite(PIN_LED_ROJA, LOW);
    digitalWrite(PIN_LED_VERDE, HIGH);
    digitalWrite(PIN_LED_AMARILLA, LOW);
    update_display("VERDE", "ENCENDIDA");
  } else if (messageTemp == "encender_amarilla") {
    digitalWrite(PIN_LED_ROJA, LOW);
    digitalWrite(PIN_LED_VERDE, LOW);
    digitalWrite(PIN_LED_AMARILLA, HIGH);
    update_display("AMARILLA", "ENCENDIDA");
  } else if (messageTemp == "reproducir_musica") {
    update_display("BUZZER", "TOCANDO");
    reproducir_musica();
  }
}

void reconnect() {
  while (!client.connected()) {
    Serial.print("Intentando conexión MQTT...");
    // ID de cliente aleatorio
    String clientId = "ESP32Client-";
    clientId += String(random(0, 1000));
    
    if (client.connect(clientId.c_str())) {
      Serial.println("conectado");
      client.subscribe(mqtt_topic_sub);
    } else {
      Serial.print("falló, rc=");
      Serial.print(client.state());
      Serial.println(" intentando de nuevo en 5 segundos");
      delay(5000);
    }
  }
}

void setup() {
  Serial.begin(115200);

  // Configuración Pines
  pinMode(PIN_LED_ROJA, OUTPUT);
  pinMode(PIN_LED_VERDE, OUTPUT);
  pinMode(PIN_LED_AMARILLA, OUTPUT);
  pinMode(PIN_BUZZER, OUTPUT);
  digitalWrite(PIN_LED_ROJA, LOW);
  digitalWrite(PIN_LED_VERDE, LOW);
  digitalWrite(PIN_LED_AMARILLA, LOW);

  // Iniciar OLED
  if(!display.begin(SSD1306_SWITCHCAPVCC, 0x3C)) { 
    Serial.println(F("Error asiganción SSD1306 OLED"));
  }
  display.clearDisplay();
  display.setTextColor(WHITE);
  display.setTextSize(1);
  display.setCursor(0, 0);
  display.println("Iniciando...");
  display.display();

  setup_wifi();
  client.setServer(mqtt_server, 1883);
  client.setCallback(callback);

  display.clearDisplay();
  display.setCursor(0, 0);
  display.println("Conectado/Listo");
  display.display();
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop();
}

