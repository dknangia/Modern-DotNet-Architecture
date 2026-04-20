using System;
using System.Collections.Generic;
using System.Text;
using TaskFlow.Domain.Enums;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Domain.Entities
{
    public class WorkTask
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;    
        public string Payload { get; set; } = string.Empty;
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public DateTime ScheduledFor { get; set; }
        public int RetryCount { get; set; }

        //Audit fields
        public  DateTime CreatedAd { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

    }
}
