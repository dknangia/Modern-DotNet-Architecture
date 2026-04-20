using System;
using System.Collections.Generic;
using System.Text;

namespace TaskFlow.Application.DTO
{
    public record CreateTaskRequest(string Name, string Payload, DateTime ScheduledFor);
    
}
