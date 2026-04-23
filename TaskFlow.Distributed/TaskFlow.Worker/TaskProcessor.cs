using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Worker
{
    public class TaskProcessor(ILogger<TaskProcessor> logger, IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            // Define these at the start of your methods in BOTH files
            var queueName = "task_queue";
            var dlxExchange = "task_failure_exchange";
            var dlqQueue = "task_queue_error";

            // 1. Declare the DLX and DLQ (Safe to do in both places)
            await channel.ExchangeDeclareAsync(dlxExchange, ExchangeType.Direct);
            await channel.QueueDeclareAsync(dlqQueue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(dlqQueue, dlxExchange, "task_failed");

            // 2. The critical "Blueprint" (Arguments)
            var arguments = new Dictionary<string, object?>
                    {
                        { "x-dead-letter-exchange", dlxExchange },
                        { "x-dead-letter-routing-key", "task_failed" }
                    };

            // 3. Declare the main queue
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments); // This MUST be the same in both projects
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                try
                {
                    logger.LogInformation("[x] Received message: {message}", message);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var outBoxdata = JsonSerializer.Deserialize<OutboxMessage>(message, options);
                    if (outBoxdata != null)
                    {
                        var taskData = JsonSerializer.Deserialize<WorkTask>(outBoxdata.Content);

                        if (taskData == null)
                        {
                            logger.LogError($"OutBox Task :{outBoxdata.Id}, doesn't have any WorkTask.");
                            return;
                        }

                        // Checking IDEMPOTENCY: If the task is already completed, we skip processing to avoid duplicate work.
                        if (taskData.Status == TaskStatus.Completed)
                        {
                            logger.LogInformation($"Task :{taskData.Id} is already completed. Skipping processing.");
                            return;
                        }

                        using (logger.BeginScope(new Dictionary<string, object> { ["TaskId"] = taskData.Id }))
                        {
                            logger.LogInformation("Processing task from queue...");
                            await ProcessTaskAsync(taskData);
                        }
                        await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }


                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Permanent failure for message. Moving to Dead Letter Queue.");
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, false, requeue: false);
                }

            };

            await channel.BasicConsumeAsync(queue: "task_queue", autoAck: false, consumer: consumer);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task ProcessTaskAsync(WorkTask taskData)
        {
            logger.LogInformation($"--------> Executing Task: {taskData.Name}");

            await Task.Delay(1000);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var dbTask = await context.WorkTasks.FindAsync(taskData.Id);
            if (dbTask != null)
            {
                dbTask.Status = TaskStatus.Completed;
                dbTask.LastUpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(); ;
            }

            logger.LogInformation($"<-------- Completed Task: {taskData.Id}");
        }
    }


}
