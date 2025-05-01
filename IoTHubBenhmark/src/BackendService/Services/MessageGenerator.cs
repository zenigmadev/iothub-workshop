using System;
using System.Text;
using Common.Models;

namespace BackendService.Services
{
    public class MessageGenerator
    {
        private readonly int _messageSizeBytes;
        private readonly Random _random;
        
        public MessageGenerator(int messageSizeBytes)
        {
            _messageSizeBytes = messageSizeBytes;
            _random = new Random();
        }
        
        public DeviceMessage GenerateMessage(string deviceId, int messageId)
        {
            // Generate basic telemetry data
            var temperature = Math.Round(20 + _random.NextDouble() * 15, 2); // Between 20-35
            var humidity = Math.Round(30 + _random.NextDouble() * 50, 2); // Between 30-80
            var pressure = Math.Round(980 + _random.NextDouble() * 40, 2); // Between 980-1020
            
            // Create payload of the requested size
            int baseSize = 150; // Approximate base JSON size
            int payloadSize = Math.Max(0, _messageSizeBytes - baseSize);
            string payload = GenerateRandomPayload(payloadSize);
            
            return new DeviceMessage
            {
                MessageId = messageId,
                DeviceId = deviceId,
                Timestamp = DateTime.UtcNow,
                Temperature = temperature,
                Humidity = humidity,
                Pressure = pressure,
                Payload = payload
            };
        }
        
        private string GenerateRandomPayload(int sizeInBytes)
        {
            if (sizeInBytes <= 0)
            {
                return string.Empty;
            }
            
            // Base64 encoding increases size by approximately 4/3
            // So we generate about 3/4 of the requested size in bytes
            int bytesToGenerate = (int)(sizeInBytes * 0.75);
            
            byte[] randomBytes = new byte[bytesToGenerate];
            _random.NextBytes(randomBytes);
            
            return Convert.ToBase64String(randomBytes);
        }
    }
}
