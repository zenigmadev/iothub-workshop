using System;

namespace Common.Models
{
    public class MessageSendMetrics
    {
        public string DeviceId { get; set; }
        public int MessageId { get; set; }
        public int MessageSize { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        
        public TimeSpan Duration => EndTime - StartTime;
    }
}
