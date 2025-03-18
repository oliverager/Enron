using EasyNetQ;
using MongoDB.Driver;
using SharedKernel.Models;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EmailIndexer.Infrastructure.Messaging
{
    public class EmailConsumer
    {
        private readonly IBus _bus;
        private readonly IMongoCollection<Email> _emailCollection;

        public EmailConsumer(IBus bus, IMongoClient mongoClient)
        {
            _bus = bus;
            var database = mongoClient.GetDatabase("EmailDB");
            _emailCollection = database.GetCollection<Email>("emails");

            // 🔥 Drop and recreate text index to ensure it's applied correctly
            Console.WriteLine("🛠️ Dropping existing text index (if any)...");
            try
            {
                _emailCollection.Indexes.DropOne("TextIndex");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ No existing text index to drop: {ex.Message}");
            }

            Console.WriteLine("🔄 Creating a new text index...");
            var indexKeys = Builders<Email>.IndexKeys.Text(e => e.Body).Text(e => e.Subject);
            var indexModel = new CreateIndexModel<Email>(indexKeys, new CreateIndexOptions { Name = "TextIndex" });
            _emailCollection.Indexes.CreateOne(indexModel);
            Console.WriteLine("✅ MongoDB Text Index Recreated Successfully.");
        }

        public void StartListening()
        {
            Console.WriteLine("🎧 Listening for messages on RabbitMQ...");

            _bus.PubSub.Subscribe<Email>("email_cleaned", email =>
            {
                Console.WriteLine($"📩 Received email from: {email.From}, Subject: {email.Subject}");

                var mongoEmail = new Email
                {
                    MessageId = email.MessageId,
                    Date = email.Date,
                    From = email.From,
                    To = email.To,
                    Cc = email.Cc,
                    Bcc = email.Bcc,
                    Subject = email.Subject,
                    Body = email.Body,
                    ProcessedAt = DateTime.UtcNow,
                    Indexed = true
                };

                _emailCollection.InsertOne(mongoEmail);
                Console.WriteLine("✅ Email stored in MongoDB.");
            });

            Console.WriteLine("🎯 Successfully subscribed to RabbitMQ queue.");
        }
    }
}
