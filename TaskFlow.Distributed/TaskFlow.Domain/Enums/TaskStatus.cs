using System;
using System.Collections.Generic;
using System.Text;

namespace TaskFlow.Domain.Enums
{
    public enum TaskStatus
    {
        Pending = 0,
        Scheduled = 1,
        InProgress = 2,
        Completed = 3,
        Failed = 4
    }
}
