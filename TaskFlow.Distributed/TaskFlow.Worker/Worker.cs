using System.Text.Json;
using RabbitMQ.Client;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace TaskFlow.Worker
{
    public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var messages = await context.OutboxMessages.Where(m => m.ProcessedAt == null)
                        .ToListAsync(stoppingToken);

                    foreach (var message in messages)
                    {
                        try
                        {
                            await PublishToQueueAsync(message);

                            message.ProcessedAt = DateTime.UtcNow;
                            logger.LogInformation("Message with Id: {Id} processed at {Time}", message.Id, message.ProcessedAt);
                        }
                        catch (Exception ex)
                        {

                            logger.LogError("Exception, {ex}, Failed to process message with Id: {Id}", ex.Message, message.Id);
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task PublishToQueueAsync(OutboxMessage message)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
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
        }
    }
}
