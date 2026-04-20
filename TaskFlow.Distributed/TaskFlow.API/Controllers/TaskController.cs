using Microsoft.AspNetCore.Mvc;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Application.DTO;
using TaskFlow.Domain.Entities;

namespace TaskFlow.API.Controllers
{
    [Route("api/[controller]")]
    public class TaskController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        public TaskController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            // 1. Create task entity
            var task = new WorkTask
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Payload = request.Payload,
                ScheduledFor = request.ScheduledFor,
                Status = Domain.Enums.TaskStatus.Pending,

            };


            // 2. Create teh outbox message
            var outBoxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "TaskCreated",
                Content = System.Text.Json.JsonSerializer.Serialize(task),
                CreatedAt = DateTime.UtcNow,

            };

            // 3. Save both to DB in a single transaction

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                _dbContext.WorkTasks.Add(task);
                _dbContext.OutboxMessages.Add(outBoxMessage);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { task.Id, Message = "Task created and saved to DB." });

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Message = "An error occurred while creating the task.", Details = ex.Message });
            }
        }
    }
}  
