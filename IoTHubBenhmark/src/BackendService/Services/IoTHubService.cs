using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Common.Models;

namespace BackendService.Services
{
    public class IoTHubService
    {
        private readonly string _connectionString;
        private ServiceClient _serviceClient;
        private bool _isConnected;
        private readonly Dictionary<string, ServiceClient> _deviceConnections;

        public IoTHubService(string connectionString)
        {
            _connectionString = connectionString;
            _isConnected = false;
            _deviceConnections = new Dictionary<string, ServiceClient>();
        }

        public async Task ConnectAsync()
        {
            if (_isConnected)
            {
                return;
            }

            try
            {
                // Create service client using AMQP protocol for better performance
                _serviceClient = ServiceClient.CreateFromConnectionString(_connectionString, TransportType.Amqp);
                
                // Open the connection
                await _serviceClient.OpenAsync();
                
                _isConnected = true;
                Console.WriteLine("Connected to IoT Hub service.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IoT Hub service connection error: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (!_isConnected || _serviceClient == null)
            {
                return;
            }

            try
            {
                await _serviceClient.CloseAsync();
                _isConnected = false;
                Console.WriteLine("IoT Hub service connection closed.");
                
                // Close all device-specific connections
                foreach (var deviceClient in _deviceConnections.Values)
                {
                    await deviceClient.CloseAsync();
                }
                _deviceConnections.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IoT Hub service connection closing error: {ex.Message}");
            }
        }

        public async Task<MessageSendMetrics> SendMessageToDeviceAsync(string deviceId, DeviceMessage messageData)
        {
            if (!_isConnected || _serviceClient == null)
            {
                throw new InvalidOperationException("Not connected to IoT Hub service.");
            }

            var metrics = new MessageSendMetrics
            {
                DeviceId = deviceId,
                MessageId = messageData.MessageId,
                MessageSize = 0,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Convert message to JSON
                string messageJson = JsonSerializer.Serialize(messageData);
                byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);
                
                // Record message size
                metrics.MessageSize = messageBytes.Length;
                
                // Create IoT Hub message
                var message = new Message(messageBytes);
                
                // Set message properties
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";
                message.MessageId = messageData.MessageId.ToString();
                
                // Add custom properties
                message.Properties.Add("source", "backend-service");
                message.Properties.Add("messageType", "telemetry");
                
                if (messageData.Temperature > 30)
                {
                    message.Properties.Add("temperatureAlert", "true");
                }
                
                // Send the message
                await _serviceClient.SendAsync(deviceId, message);
                
                // Record end time
                metrics.EndTime = DateTime.UtcNow;
                metrics.IsSuccess = true;
            }
            catch (Exception ex)
            {
                metrics.EndTime = DateTime.UtcNow;
                metrics.IsSuccess = false;
                metrics.ErrorMessage = ex.Message;
                
                Console.WriteLine($"Error sending message to device {deviceId}: {ex.Message}");
            }

            return metrics;
        }

        public async Task<ServiceClient> GetDeviceConnectionAsync(string deviceId)
        {
            if (_deviceConnections.TryGetValue(deviceId, out var existingClient))
            {
                return existingClient;
            }

            try
            {
                // Create a new connection for this device
                var deviceClient = ServiceClient.CreateFromConnectionString(_connectionString, TransportType.Amqp);
                await deviceClient.OpenAsync();
                
                _deviceConnections[deviceId] = deviceClient;
                Console.WriteLine($"Created dedicated connection for device {deviceId}");
                
                return deviceClient;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating connection for device {deviceId}: {ex.Message}");
                throw;
            }
        }

        public async Task<MessageSendMetrics> SendMessageToDeviceWithDedicatedConnectionAsync(string deviceId, DeviceMessage messageData)
        {
            var metrics = new MessageSendMetrics
            {
                DeviceId = deviceId,
                MessageId = messageData.MessageId,
                MessageSize = 0,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Get or create a dedicated connection for this device
                var deviceClient = await GetDeviceConnectionAsync(deviceId);
                
                // Convert message to JSON
                string messageJson = JsonSerializer.Serialize(messageData);
                byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);
                
                // Record message size
                metrics.MessageSize = messageBytes.Length;
                
                // Create IoT Hub message
                var message = new Message(messageBytes);
                
                // Set message properties
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";
                message.MessageId = messageData.MessageId.ToString();
                
                // Add custom properties
                message.Properties.Add("source", "backend-service");
                message.Properties.Add("messageType", "telemetry");
                
                if (messageData.Temperature > 30)
                {
                    message.Properties.Add("temperatureAlert", "true");
                }
                
                // Send the message
                await deviceClient.SendAsync(deviceId, message);
                
                // Record end time
                metrics.EndTime = DateTime.UtcNow;
                metrics.IsSuccess = true;
            }
            catch (Exception ex)
            {
                metrics.EndTime = DateTime.UtcNow;
                metrics.IsSuccess = false;
                metrics.ErrorMessage = ex.Message;
                
                Console.WriteLine($"Error sending message to device {deviceId} with dedicated connection: {ex.Message}");
            }

            return metrics;
        }
    }
}
