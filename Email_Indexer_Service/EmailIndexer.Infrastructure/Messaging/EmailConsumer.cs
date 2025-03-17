using EasyNetQ;
using SharedKernel.Models;
using EmailIndexer.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EmailIndexer.Infrastructure.Messaging
{
    public class EmailConsumer
    {
        private readonly IBus _bus;
        private readonly IServiceScopeFactory _scopeFactory;

        public EmailConsumer(IBus bus, IServiceScopeFactory scopeFactory)
        {
            _bus = bus;
            _scopeFactory = scopeFactory;
        }

        public void StartListening()
        {
            Console.WriteLine("✅ Email Consumer started, waiting for messages...");

            _bus.PubSub.Subscribe<Email>("email_cleaned", email =>
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var indexerService = scope.ServiceProvider.GetRequiredService<EmailIndexerService>();
                    Console.WriteLine($"📩 Received email from: {email.From}, Subject: {email.Subject}, Date: {email.Date}");

                    indexerService.IndexEmail(email);
                }
            });

            Console.WriteLine("🎯 Successfully subscribed to RabbitMQ queue.");
        }
    }
}