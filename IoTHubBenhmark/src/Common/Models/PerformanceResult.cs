using System;
using System.Collections.Generic;

namespace Common.Models
{
    public class PerformanceResult
    {
        public string ScenarioName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalMessages { get; set; }
        public int SuccessMessages { get; set; }
        public int FailedMessages { get; set; }
        public double AverageDuration { get; set; }
        public double MinDuration { get; set; }
        public double MaxDuration { get; set; }
        public double MessagesPerSecond { get; set; }
        
        public TimeSpan TotalDuration => EndTime - StartTime;
    }
}
