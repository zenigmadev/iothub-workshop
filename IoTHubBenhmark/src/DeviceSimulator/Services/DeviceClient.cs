using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Common.Models;

namespace DeviceSimulator.Services
{
    public class DeviceClient
    {
        private readonly string _deviceId;
        private readonly string _connectionString;
        private Microsoft.Azure.Devices.Client.DeviceClient _deviceClient;
        private bool _isConnected;
        private CancellationTokenSource _cancellationTokenSource;
        
        // For latency tracking
        private List<MessageLatencyMetric> _latencyMetrics = new List<MessageLatencyMetric>();
        private DateTime _lastMessageReceivedTime = DateTime.MinValue;
        private bool _timeoutDetected = false;
        private readonly TimeSpan _messageTimeout = TimeSpan.FromSeconds(10);
        private Timer _timeoutTimer;
        
        // For one-time display
        private bool _averageDisplayed = false;
        
        // Stack-based metric storage for continuous average calculation
        private Queue<MessageLatencyMetric> _metricHistory = new Queue<MessageLatencyMetric>();
        private const int MAX_HISTORY_SIZE = 100; // Keep last 100 messages for history
        
        // For current window metrics
        private List<MessageLatencyMetric> _pendingMetrics = new List<MessageLatencyMetric>();
        
        // Flag to track if we've received any messages since last display
        private bool _receivedNewMessagesSinceDisplay = false;

        public string DeviceId => _deviceId;

        public DeviceClient(string deviceId, string connectionString)
        {
            _deviceId = deviceId;
            _connectionString = connectionString;
            _isConnected = false;
        }

        public List<MessageLatencyMetric> GetLatencyMetrics()
        {
            return _latencyMetrics;
        }

        public bool HasTimeoutOccurred()
        {
            return _timeoutDetected;
        }
        
        public bool HasAverageBeenDisplayed()
        {
            return _averageDisplayed;
        }
        
        public void SetAverageDisplayed(bool displayed)
        {
            _averageDisplayed = displayed;
            
            // If we're marking as displayed, also reset the new messages flag
            if (displayed)
            {
                _receivedNewMessagesSinceDisplay = false;
            }
        }
        
        public void ResetMetrics()
        {
            // Instead of clearing, move pending metrics to history
            foreach (var metric in _pendingMetrics)
            {
                AddToMetricHistory(metric);
            }
            
            // Now clear the pending metrics
            _pendingMetrics.Clear();
            Console.WriteLine($"Device {_deviceId} metrics moved to history after displaying average");
        }
        
        private void AddToMetricHistory(MessageLatencyMetric metric)
        {
            // Add to history queue
            _metricHistory.Enqueue(metric);
            
            // Keep history size limited
            while (_metricHistory.Count > MAX_HISTORY_SIZE)
            {
                _metricHistory.Dequeue();
            }
        }

        public async Task ConnectAsync()
        {
            if (_isConnected)
            {
                return;
            }

            try
            {
                // Create device client using MQTT protocol
                _deviceClient = Microsoft.Azure.Devices.Client.DeviceClient.CreateFromConnectionString(_connectionString, TransportType.Mqtt);
                
                // Set connection status change handler
                _deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);
                
                // Open the connection
                await _deviceClient.OpenAsync();
                
                _isConnected = true;
                Console.WriteLine($"Device {_deviceId} connected to IoT Hub.");
                
                // Initialize timeout timer - check every 1 second
                _timeoutTimer = new Timer(CheckMessageTimeout, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Device {_deviceId} connection error: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (!_isConnected || _deviceClient == null)
            {
                return;
            }

            try
            {
                // Stop timeout timer
                _timeoutTimer?.Dispose();
                
                // Stop message receiving
                await StopReceivingMessagesAsync();
                
                // Close the connection
                await _deviceClient.CloseAsync();
                
                _isConnected = false;
                Console.WriteLine($"Device {_deviceId} disconnected from IoT Hub.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Device {_deviceId} disconnection error: {ex.Message}");
            }
        }

        public async Task StartReceivingMessagesAsync()
        {
            if (!_isConnected || _deviceClient == null)
            {
                throw new InvalidOperationException($"Device {_deviceId} is not connected to IoT Hub.");
            }

            try
            {
                // Create cancellation token source
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Set message handler
                await _deviceClient.SetReceiveMessageHandlerAsync(MessageHandler, _deviceClient);
                
                Console.WriteLine($"Device {_deviceId} started receiving messages.");
                
                // Initialize last message time
                _lastMessageReceivedTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Device {_deviceId} error starting message reception: {ex.Message}");
                throw;
            }
        }

        public async Task StopReceivingMessagesAsync()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
                
                // Reset message handler
                await _deviceClient.SetReceiveMessageHandlerAsync(null, null);
                
                Console.WriteLine($"Device {_deviceId} stopped receiving messages.");
            }
        }

        private async Task<MessageResponse> MessageHandler(Message message, object userContext)
        {
            try
            {
                // Update last message received time
                _lastMessageReceivedTime = DateTime.UtcNow;
                
                // Reset timeout flag when new message arrives
                _timeoutDetected = false;
                
                // Mark that we've received new messages since last display
                _receivedNewMessagesSinceDisplay = true;
                
                // Get message content
                string messageContent = Encoding.UTF8.GetString(message.GetBytes());
                
                // Parse message as JSON
                var deviceMessage = JsonSerializer.Deserialize<DeviceMessage>(messageContent);
                
                // Calculate latency
                var receiveTime = DateTime.UtcNow;
                var latency = receiveTime - deviceMessage.Timestamp;
                
                // Create metric
                var metric = new MessageLatencyMetric
                {
                    MessageId = deviceMessage.MessageId,
                    SendTime = deviceMessage.Timestamp,
                    ReceiveTime = receiveTime,
                    Latency = latency
                };
                
                // Add to overall latency metrics
                _latencyMetrics.Add(metric);
                
                // Add to pending metrics for timeout calculation
                _pendingMetrics.Add(metric);
                
                // Get message properties
                var properties = message.Properties;
                string messageType = properties.ContainsKey("messageType") ? properties["messageType"] : "unknown";
                
                // Process message
                Console.WriteLine($"Device {_deviceId} received message: ID={deviceMessage.MessageId}, Type={messageType}");
                Console.WriteLine($"  Temperature: {deviceMessage.Temperature:F2}, Humidity: {deviceMessage.Humidity:F2}, Pressure: {deviceMessage.Pressure:F2}");
                Console.WriteLine($"  Latency: {latency.TotalMilliseconds:F2} ms");
                
                // Calculate and display average latencies
                double? pendingAvg = GetPendingMetricsAverage();
                double? historyAvg = GetHistoryMetricsAverage();
                double? overallAvg = GetOverallAverageLatency();
                
                Console.WriteLine($"  Pending metrics: {_pendingMetrics.Count} (Avg: {(pendingAvg.HasValue ? $"{pendingAvg.Value:F2} ms" : "N/A")})");
                Console.WriteLine($"  History metrics: {_metricHistory.Count} (Avg: {(historyAvg.HasValue ? $"{historyAvg.Value:F2} ms" : "N/A")})");
                Console.WriteLine($"  Overall average latency: {(overallAvg.HasValue ? $"{overallAvg.Value:F2} ms" : "N/A")}");
                
                // Check for temperature alert
                if (deviceMessage.Temperature > 30)
                {
                    Console.WriteLine($"  [ALERT] High temperature detected: {deviceMessage.Temperature:F2}");
                }
                
                // Complete the message to remove it from the queue
                await _deviceClient.CompleteAsync(message);
                
                return MessageResponse.Completed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Device {_deviceId} message processing error: {ex.Message}");
                
                // Abandon the message to return it to the queue
                await _deviceClient.AbandonAsync(message);
                
                return MessageResponse.Abandoned;
            }
        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine($"Device {_deviceId} connection status changed: {status}, Reason: {reason}");
            
            _isConnected = (status == ConnectionStatus.Connected);
            
            if (!_isConnected && reason != ConnectionStatusChangeReason.Client_Close)
            {
                // Automatic reconnection strategy when connection is lost
                Task.Run(async () =>
                {
                    Console.WriteLine($"Device {_deviceId} attempting to reconnect...");
                    
                    try
                    {
                        await Task.Delay(5000); // Wait 5 seconds
                        await ConnectAsync();
                        
                        if (_isConnected)
                        {
                            await StartReceivingMessagesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Device {_deviceId} reconnection error: {ex.Message}");
                    }
                });
            }
        }
        
        private void CheckMessageTimeout(object state)
        {
            var now = DateTime.UtcNow;
            var timeSinceLastMessage = now - _lastMessageReceivedTime;
            
            // Check if we've received messages before and if it's been more than the timeout period
            if (_lastMessageReceivedTime != DateTime.MinValue && 
                timeSinceLastMessage > _messageTimeout && 
                !_timeoutDetected && 
                (_pendingMetrics.Count > 0 || _metricHistory.Count > 0) &&
                (_receivedNewMessagesSinceDisplay || !_averageDisplayed))
            {
                _timeoutDetected = true;
                Console.WriteLine($"Device {_deviceId} timeout detected! No messages received for {timeSinceLastMessage.TotalSeconds:F1} seconds.");
                
                // Calculate and display average latencies
                double? pendingAvg = GetPendingMetricsAverage();
                double? historyAvg = GetHistoryMetricsAverage();
                double? overallAvg = GetOverallAverageLatency();
                
                Console.WriteLine($"Pending metrics: {_pendingMetrics.Count} (Avg: {(pendingAvg.HasValue ? $"{pendingAvg.Value:F2} ms" : "N/A")})");
                Console.WriteLine($"History metrics: {_metricHistory.Count} (Avg: {(historyAvg.HasValue ? $"{historyAvg.Value:F2} ms" : "N/A")})");
                Console.WriteLine($"Overall average latency: {(overallAvg.HasValue ? $"{overallAvg.Value:F2} ms" : "N/A")}");
                
                // Mark that we've displayed the average (will be set to true in Program.cs)
                // This ensures we only display the average once until new messages arrive
                _averageDisplayed = true;
            }
            else if (_lastMessageReceivedTime != DateTime.MinValue && 
                     timeSinceLastMessage <= _messageTimeout && 
                     _averageDisplayed && 
                     _receivedNewMessagesSinceDisplay)
            {
                // If we've received a new message and previously displayed the average,
                // reset the displayed flag so we can display again after the next timeout
                _averageDisplayed = false;
                Console.WriteLine($"Device {_deviceId} received new messages, will display metrics on next timeout");
            }
        }
        
        public double? GetPendingMetricsAverage()
        {
            // First try to use pending metrics
            if (_pendingMetrics.Count > 0)
            {
                return _pendingMetrics.Average(m => m.Latency.TotalMilliseconds);
            }
            
            return null;
        }
        
        public double? GetHistoryMetricsAverage()
        {
            // Calculate average from history
            if (_metricHistory.Count > 0)
            {
                return _metricHistory.Average(m => m.Latency.TotalMilliseconds);
            }
            
            return null;
        }
        
        public double? GetOverallAverageLatency()
        {
            // Combine pending metrics and history for overall average
            var allMetrics = new List<MessageLatencyMetric>(_pendingMetrics);
            allMetrics.AddRange(_metricHistory);
            
            if (allMetrics.Count > 0)
            {
                return allMetrics.Average(m => m.Latency.TotalMilliseconds);
            }
            
            return null;
        }
        
        public int GetPendingMetricsCount()
        {
            return _pendingMetrics.Count;
        }
        
        public int GetHistoryMetricsCount()
        {
            return _metricHistory.Count;
        }
    }
    
    public class MessageLatencyMetric
    {
        public int MessageId { get; set; }
        public DateTime SendTime { get; set; }
        public DateTime ReceiveTime { get; set; }
        public TimeSpan Latency { get; set; }
    }
}
