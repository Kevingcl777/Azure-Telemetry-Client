using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace Test
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static DeviceClient s_deviceClient;
        private readonly static string s_connectionString01 = "[Your connection to AZURE]";

        private SerialDevice UartPort;
        private DataReader DataReaderObject;
        private DataWriter DataWriterObject;
        private CancellationTokenSource ReadCancellationTokenSource;

        public MainPage()
        {
            this.InitializeComponent();
            AsyncHelper.RunSync(() => Initialise());
            Console.ReadLine();
        }

        private async Task Initialise()
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector("UART0");
                var dis = await DeviceInformation.FindAllAsync(aqs);
                if (dis.Count == 0)
                {
                    throw new Exception("No UART devices found.");
                }

                UartPort = await SerialDevice.FromIdAsync(dis[0].Id);

                // Configure serial settings
                UartPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                UartPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                UartPort.BaudRate = 9600;
                UartPort.Parity = SerialParity.None;
                UartPort.StopBits = SerialStopBitCount.One;
                UartPort.DataBits = 8;

                DataReaderObject = new DataReader(UartPort.InputStream)
                {
                    InputStreamOptions = InputStreamOptions.Partial
                };
                DataWriterObject = new DataWriter(UartPort.OutputStream);

                s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString01, TransportType.Mqtt);

                StartReceive();
            }
            catch (Exception ex)
            {
                throw new Exception("UART Initialisation Error", ex);
            }
        }

        public async void StartReceive()
        {
            ReadCancellationTokenSource = new CancellationTokenSource();

            while (!ReadCancellationTokenSource.Token.IsCancellationRequested && UartPort != null)
            {
                await Listen();
            }
        }

        private async Task Listen()
        {
            const int NUMBER_OF_BYTES_TO_RECEIVE = 1;

            try
            {
                if (UartPort != null)
                {
                    while (!ReadCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        UInt32 bytesRead = await DataReaderObject.LoadAsync(NUMBER_OF_BYTES_TO_RECEIVE).AsTask();

                        if (ReadCancellationTokenSource.Token.IsCancellationRequested || UartPort == null)
                            break;

                        if (bytesRead > 0)
                        {
                            byte[] receiveData = new byte[NUMBER_OF_BYTES_TO_RECEIVE];
                            DataReaderObject.ReadBytes(receiveData);

                            string receivedString = Encoding.UTF8.GetString(receiveData);
                            if (receivedString.Length >= 10)
                            {
                                var df1 = int.Parse(receivedString.Substring(6, 2));
                                var df2 = int.Parse(receivedString.Substring(8, 2));
                                var co2 = df1 * 256.0 + df2;

                                await SendDataCloudMessagesAsync("ppm", co2, "CO2W10", "CO2Sensor");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (ReadCancellationTokenSource != null && !ReadCancellationTokenSource.IsCancellationRequested)
                    ReadCancellationTokenSource.Cancel();

                System.Diagnostics.Debug.WriteLine("UART ReadAsync Exception: {0}", e.Message);
                await SendDataCloudMessagesAsync("ppm", 788, "CO2W10Err", "CO2Sensor");
            }
        }

        public async Task SendBytesAsync(byte[] txData)
        {
            try
            {
                DataWriterObject.WriteBytes(txData);
                await DataWriterObject.StoreAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("UART Tx Error", ex);
            }
        }

        private static async Task SendDataCloudMessagesAsync(string unit, double val, string key, string deviceId)
        {
            try
            {
                var dataIn = new
                {
                    unit = unit,
                    val = val,
                    key = key
                };

                var telemetryDataPoint = new
                {
                    data = dataIn,
                    deviceId = deviceId
                };

                string messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Cloud Send Error: {0}", ex.Message);
                throw;
            }
        }
    }

    public static class AsyncHelper
    {
        private static readonly TaskFactory _taskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
            => _taskFactory.StartNew(func).Unwrap().GetAwaiter().GetResult();

        public static void RunSync(Func<Task> func)
            => _taskFactory.StartNew(func).Unwrap().GetAwaiter().GetResult();
    }
}
