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

            await channel.QueueDeclareAsync(queue: "task_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
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

                        if (taskData.Status == TaskStatus.Completed)
                        {
                            logger.LogInformation($"Task :{taskData.Id} is already completed. Skipping processing.");
                            return;
                        }

                        await ProcessTaskAsync(taskData);
                        await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }


                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing message: {message}");
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, false, requeue: true);
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
