using System;
using System.Text.Json.Serialization;

namespace Common.Models
{
    public class DeviceMessage
    {
        [JsonPropertyName("messageId")]
        public int MessageId { get; set; }
        
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
        
        [JsonPropertyName("humidity")]
        public double Humidity { get; set; }
        
        [JsonPropertyName("pressure")]
        public double Pressure { get; set; }
        
        [JsonPropertyName("payload")]
        public string Payload { get; set; }
    }
}
