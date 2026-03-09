using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BackendService.Services;
using Common.Models;

namespace BackendService
{
    class Program
    {
        static int Main(string[] args)
        {
            // Load configuration file
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Define command line arguments
            var rootCommand = new RootCommand("IoT Hub Performance Test Backend Service");
            
            var scenarioOption = new Option<int>("--scenario") 
            { 
                Description = "Test scenario number (1-5)",
                DefaultValueFactory = _ => 1
            };
            
            rootCommand.Options.Add(scenarioOption);
            
            rootCommand.SetAction(parseResult =>
            {
                var scenario = parseResult.GetValue(scenarioOption);
                Console.WriteLine($"Starting scenario {scenario}...");
                
                try
                {
                    // Get scenario configuration
                    var scenarioConfig = configuration.GetSection($"TestSettings:Scenario{scenario}");
                    
                    if (scenarioConfig == null || !scenarioConfig.Exists())
                    {
                        Console.WriteLine($"Error: Configuration for scenario {scenario} not found.");
                        return;
                    }
                    
                    int deviceCount = int.Parse(scenarioConfig["DeviceCount"]);
                    int messagesPerDevice = int.Parse(scenarioConfig["MessagesPerDevice"]);
                    int messageInterval = int.Parse(scenarioConfig["MessageInterval"]);
                    int messageSizeBytes = int.Parse(scenarioConfig["MessageSizeBytes"]);
                    bool useMultipleConnections = bool.Parse(scenarioConfig["UseMultipleConnections"] ?? "false");
                    
                    // Get IoT Hub configuration
                    var iotHubConfig = configuration.GetSection("IoTHub");
                    string connectionString = iotHubConfig["ConnectionString"];
                    string devicePrefix = iotHubConfig["DevicePrefix"];
                    
                    // Run the test scenario
                    switch (scenario)
                    {
                        case 1:
                            // Scenario 1: Single Connection, Serial Messages to One Device
                            RunScenario1Async(connectionString, devicePrefix, messagesPerDevice, messageInterval, messageSizeBytes).GetAwaiter().GetResult();
                            break;
                        
                        case 2:
                            // Scenario 2: Single Connection, Parallel Messages to Multiple Devices
                            RunScenario2Async(connectionString, devicePrefix, deviceCount, messagesPerDevice, messageSizeBytes).GetAwaiter().GetResult();
                            break;
                        
                        case 3:
                            // Scenario 3: Single Connection, Serial Messages to Multiple Devices
                            RunScenario3Async(connectionString, devicePrefix, deviceCount, messagesPerDevice, messageInterval, messageSizeBytes).GetAwaiter().GetResult();
                            break;
                        
                        case 4:
                            // Scenario 4: Multiple Connections, Parallel Messages to Multiple Devices
                            RunScenario4Async(connectionString, devicePrefix, deviceCount, messagesPerDevice, messageSizeBytes).GetAwaiter().GetResult();
                            break;
                        
                        case 5:
                            // Scenario 5: Multiple Connections, Serial Messages to Multiple Devices
                            RunScenario5Async(connectionString, devicePrefix, deviceCount, messagesPerDevice, messageInterval, messageSizeBytes).GetAwaiter().GetResult();
                            break;
                        
                        default:
                            Console.WriteLine($"Error: Unknown scenario number: {scenario}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            });
            
            return rootCommand.Parse(args).Invoke();
        }
        
        // Scenario 1: Single Connection, Serial Messages to One Device
        private static async Task RunScenario1Async(string connectionString, string devicePrefix, int messageCount, int messageInterval, int messageSizeBytes)
        {
            Console.WriteLine("Scenario 1: Single Connection, Serial Messages to One Device");
            Console.WriteLine($"Device: {devicePrefix}1");
            Console.WriteLine($"Message count: {messageCount}");
            Console.WriteLine($"Message interval: {messageInterval} ms");
            Console.WriteLine($"Message size: {messageSizeBytes} bytes");
            
            // For performance measurement
            var performanceMetrics = new PerformanceMetrics("Scenario 1");
            performanceMetrics.Start();
            
            // Create IoT Hub service
            var iotHubService = new IoTHubService(connectionString);
            await iotHubService.ConnectAsync();
            
            // Message generator
            var messageGenerator = new MessageGenerator(messageSizeBytes);
            
            // Send messages
            string deviceId = $"{devicePrefix}1";
            for (int i = 1; i <= messageCount; i++)
            {
                var messageData = messageGenerator.GenerateMessage(deviceId, i);
                
                var messageSendMetrics = await iotHubService.SendMessageToDeviceAsync(deviceId, messageData);
                performanceMetrics.AddMessageMetrics(messageSendMetrics);
                
                if (i % 50 == 0)
                {
                    Console.WriteLine($"{i} messages sent...");
                    performanceMetrics.PrintInterimReport();
                }
                
                if (messageInterval > 0)
                {
                    await Task.Delay(messageInterval);
                }
            }
            
            // Close connection
            await iotHubService.DisconnectAsync();
            
            // Show performance report
            performanceMetrics.Stop();
            performanceMetrics.PrintFinalReport();
        }
        
        // Scenario 2: Single Connection, Parallel Messages to Multiple Devices
        private static async Task RunScenario2Async(string connectionString, string devicePrefix, int deviceCount, int messagesPerDevice, int messageSizeBytes)
        {
            Console.WriteLine("Scenario 2: Single Connection, Parallel Messages to Multiple Devices");
            Console.WriteLine($"Device count: {deviceCount}");
            Console.WriteLine($"Messages per device: {messagesPerDevice}");
            Console.WriteLine($"Total message count: {deviceCount * messagesPerDevice}");
            Console.WriteLine($"Message size: {messageSizeBytes} bytes");
            
            // For performance measurement
            var performanceMetrics = new PerformanceMetrics("Scenario 2");
            performanceMetrics.Start();
            
            // Create IoT Hub service with a single connection
            var iotHubService = new IoTHubService(connectionString);
            await iotHubService.ConnectAsync();
            
            // Message generator
            var messageGenerator = new MessageGenerator(messageSizeBytes);
            
            // Parallel message sending per device
            var tasks = new List<Task>();
            for (int deviceIndex = 0; deviceIndex < deviceCount; deviceIndex++)
            {
                string deviceId = $"{devicePrefix}{deviceIndex + 1}";
                tasks.Add(Task.Run(async () =>
                {
                    for (int i = 1; i <= messagesPerDevice; i++)
                    {
                        var messageData = messageGenerator.GenerateMessage(deviceId, i);
                        
                        var messageSendMetrics = await iotHubService.SendMessageToDeviceAsync(deviceId, messageData);
                        performanceMetrics.AddMessageMetrics(messageSendMetrics);
                        
                        if (i % 10 == 0)
                        {
                            Console.WriteLine($"Device {deviceId}: {i} messages sent");
                        }
                    }
                    
                    Console.WriteLine($"Device {deviceId}: All messages sent");
                }));
            }
            
            // Wait for all tasks to complete
            await Task.WhenAll(tasks);
            
            // Close connection
            await iotHubService.DisconnectAsync();
            
            // Show performance report
            performanceMetrics.Stop();
            performanceMetrics.PrintFinalReport();
        }
        
        // Scenario 3: Single Connection, Serial Messages to Multiple Devices
        private static async Task RunScenario3Async(string connectionString, string devicePrefix, int deviceCount, int messagesPerDevice, int messageInterval, int messageSizeBytes)
        {
            Console.WriteLine("Scenario 3: Single Connection, Serial Messages to Multiple Devices");
            Console.WriteLine($"Device count: {deviceCount}");
            Console.WriteLine($"Messages per device: {messagesPerDevice}");
            Console.WriteLine($"Total message count: {deviceCount * messagesPerDevice}");
            Console.WriteLine($"Message interval: {messageInterval} ms");
            Console.WriteLine($"Message size: {messageSizeBytes} bytes");
            
            // For performance measurement
            var performanceMetrics = new PerformanceMetrics("Scenario 3");
            performanceMetrics.Start();
            
            // Create IoT Hub service with a single connection
            var iotHubService = new IoTHubService(connectionString);
            await iotHubService.ConnectAsync();
            
            // Message generator
            var messageGenerator = new MessageGenerator(messageSizeBytes);
            
            // Send messages sequentially for each device
            for (int deviceIndex = 0; deviceIndex < deviceCount; deviceIndex++)
            {
                string deviceId = $"{devicePrefix}{deviceIndex + 1}";
                Console.WriteLine($"Starting message sending for device {deviceId}...");
                
                for (int i = 1; i <= messagesPerDevice; i++)
                {
                    var messageData = messageGenerator.GenerateMessage(deviceId, i);
                    
                    var messageSendMetrics = await iotHubService.SendMessageToDeviceAsync(deviceId, messageData);
                    performanceMetrics.AddMessageMetrics(messageSendMetrics);
                    
                    if (i % 10 == 0)
                    {
                        Console.WriteLine($"Device {deviceId}: {i} messages sent");
                    }
                    
                    if (messageInterval > 0)
                    {
                        await Task.Delay(messageInterval);
                    }
                }
                
                Console.WriteLine($"Device {deviceId}: All messages sent");
            }
            
            // Close connection
            await iotHubService.DisconnectAsync();
            
            // Show performance report
            performanceMetrics.Stop();
            performanceMetrics.PrintFinalReport();
        }
        
        // Scenario 4: Multiple Connections, Parallel Messages to Multiple Devices
        private static async Task RunScenario4Async(string connectionString, string devicePrefix, int deviceCount, int messagesPerDevice, int messageSizeBytes)
        {
            Console.WriteLine("Scenario 4: Multiple Connections, Parallel Messages to Multiple Devices");
            Console.WriteLine($"Device count: {deviceCount}");
            Console.WriteLine($"Messages per device: {messagesPerDevice}");
            Console.WriteLine($"Total message count: {deviceCount * messagesPerDevice}");
            Console.WriteLine($"Message size: {messageSizeBytes} bytes");
            
            // For performance measurement
            var performanceMetrics = new PerformanceMetrics("Scenario 4");
            performanceMetrics.Start();
            
            // Create IoT Hub service
            var iotHubService = new IoTHubService(connectionString);
            await iotHubService.ConnectAsync();
            
            // Message generator
            var messageGenerator = new MessageGenerator(messageSizeBytes);
            
            // Parallel message sending per device with dedicated connections
            var tasks = new List<Task>();
            for (int deviceIndex = 0; deviceIndex < deviceCount; deviceIndex++)
            {
                string deviceId = $"{devicePrefix}{deviceIndex + 1}";
                tasks.Add(Task.Run(async () =>
                {
                    for (int i = 1; i <= messagesPerDevice; i++)
                    {
                        var messageData = messageGenerator.GenerateMessage(deviceId, i);
                        
                        var messageSendMetrics = await iotHubService.SendMessageToDeviceWithDedicatedConnectionAsync(deviceId, messageData);
                        performanceMetrics.AddMessageMetrics(messageSendMetrics);
                        
                        if (i % 10 == 0)
                        {
                            Console.WriteLine($"Device {deviceId}: {i} messages sent");
                        }
                    }
                    
                    Console.WriteLine($"Device {deviceId}: All messages sent");
                }));
            }
            
            // Wait for all tasks to complete
            await Task.WhenAll(tasks);
            
            // Close all connections
            await iotHubService.DisconnectAsync();
            
            // Show performance report
            performanceMetrics.Stop();
            performanceMetrics.PrintFinalReport();
        }
        
        // Scenario 5: Multiple Connections, Serial Messages to Multiple Devices
        private static async Task RunScenario5Async(string connectionString, string devicePrefix, int deviceCount, int messagesPerDevice, int messageInterval, int messageSizeBytes)
        {
            Console.WriteLine("Scenario 5: Multiple Connections, Serial Messages to Multiple Devices");
            Console.WriteLine($"Device count: {deviceCount}");
            Console.WriteLine($"Messages per device: {messagesPerDevice}");
            Console.WriteLine($"Total message count: {deviceCount * messagesPerDevice}");
            Console.WriteLine($"Message interval: {messageInterval} ms");
            Console.WriteLine($"Message size: {messageSizeBytes} bytes");
            
            // For performance measurement
            var performanceMetrics = new PerformanceMetrics("Scenario 5");
            performanceMetrics.Start();
            
            // Create IoT Hub service
            var iotHubService = new IoTHubService(connectionString);
            await iotHubService.ConnectAsync();
            
            // Message generator
            var messageGenerator = new MessageGenerator(messageSizeBytes);
            
            // Send messages sequentially for each device with dedicated connections
            for (int deviceIndex = 0; deviceIndex < deviceCount; deviceIndex++)
            {
                string deviceId = $"{devicePrefix}{deviceIndex + 1}";
                Console.WriteLine($"Starting message sending for device {deviceId}...");
                
                for (int i = 1; i <= messagesPerDevice; i++)
                {
                    var messageData = messageGenerator.GenerateMessage(deviceId, i);
                    
                    var messageSendMetrics = await iotHubService.SendMessageToDeviceWithDedicatedConnectionAsync(deviceId, messageData);
                    performanceMetrics.AddMessageMetrics(messageSendMetrics);
                    
                    if (i % 10 == 0)
                    {
                        Console.WriteLine($"Device {deviceId}: {i} messages sent");
                    }
                    
                    if (messageInterval > 0)
                    {
                        await Task.Delay(messageInterval);
                    }
                }
                
                Console.WriteLine($"Device {deviceId}: All messages sent");
            }
            
            // Close all connections
            await iotHubService.DisconnectAsync();
            
            // Show performance report
            performanceMetrics.Stop();
            performanceMetrics.PrintFinalReport();
        }
    }
}

