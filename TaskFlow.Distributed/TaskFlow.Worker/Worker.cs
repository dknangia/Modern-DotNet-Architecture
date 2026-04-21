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
                    var messages = await context.OutboxMessages.Where(m=>m.ProcessedAt == null)
                        .ToListAsync(stoppingToken);

                    foreach(var message in messages)
                    {
                        try
                        {
                            await PublishToQueueAsync(message);

                            message.ProcessedAt = DateTime.UtcNow;
                            logger.LogInformation("Message with Id: {Id} processed at {Time}", message.Id, message.ProcessedAt);
                        }
                        catch (Exception)
                        {

                            logger.LogError("Failed to process message with Id: {Id}", message.Id);
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task PublishToQueueAsync(OutboxMessage message)
        {
           var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queue: "task_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            await channel.BasicPublishAsync(exchange: "", routingKey: "task_queue", body: body);
        }
    }
}
