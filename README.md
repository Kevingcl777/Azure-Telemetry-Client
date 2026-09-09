# Azure-Telemetry-Client
An old project where I tested a UWP C# app on a RPI 2B to upload sensor data to the Azure Data Lake
# Universal Windows Platform (UWP) IoT Azure Telemetry Client

This project is a UWP C# application designed for Windows IoT devices (such as Windows 10 IoT Core). It reads sensor data via a UART serial interface (e.g., a multi-gas or air quality sensor providing CO2 metrics) and transmits the telemetry data directly to **Microsoft Azure IoT Hub** using the MQTT protocol.

## Features

- **UART Serial Communication:** Initializes and communicates with hardware devices over serial ports (`UART0`) using Windows Runtime (`Windows.Devices.SerialCommunication`).
- **Azure IoT Hub Integration:** Connects securely to Azure IoT Hub using connection strings and the `Microsoft.Azure.Devices.Client` library.
- **JSON Telemetry Payload:** Formats sensor readings into structured JSON objects using **Newtonsoft.Json** before sending them upstream.
- **Asynchronous Synchronization Helper:** Includes an `AsyncHelper` utility class to bridge asynchronous operations safely within synchronous execution contexts.
- **Fault Tolerance & Error Recovery:** Handles unexpected serial disconnections via cancellation tokens and robust try-catch-finally loops.

---

## Code Architecture

- **`MainPage`**: The primary UWP page lifecycle handler that sets up hardware communication on load.
- **`Initialise()`**: Discovers available UART devices via device selector AQS strings, configures connection settings (9600 baud, 8 data bits, no parity, 1 stop bit), and instantiates readers/writers.
- **`StartReceive()` & `Listen()`**: Runs an ongoing background loop to capture incoming byte streams from the connected hardware sensor, parsing incoming frames for metrics like CO2 concentration.
- **`SendDataCloudMessagesAsync()`**: Serializes metrics into standard JSON payloads (`unit`, `val`, `key`, and `deviceId`) and publishes them asynchronously via MQTT to Azure IoT Hub.
- **`AsyncHelper`**: Executes asynchronous tasks synchronously (`RunSync`) to accommodate legacy initialization paths.

## Prerequisites

- **Visual Studio** with the **Universal Windows Platform development** workload installed.
- **Windows 10 IoT Core** compatible device (Raspberry Pi, NXP, etc.) or development environment.
- **Microsoft Azure Subscription** with an active IoT Hub instance.
- **NuGet Packages:**
  - `Microsoft.Azure.Devices.Client`
  - `Newtonsoft.Json`

## Configuration

Before running the application, update the Azure IoT Hub connection string placeholder in `MainPage.xaml.cs`:

```csharp
private readonly static string s_connectionString01 = "HostName=your-hub.azure-devices.net;DeviceId=your-device;SharedAccessKey=your-key";
