using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace SharedKernel.Models
{
    public class Email
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("messageId")]
        public string MessageId { get; set; }

        [BsonElement("date")] 
        public string Date { get; set; } = "";

        [BsonElement("from")]
        public string From { get; set; }

        [BsonElement("to")]
        public string To { get; set; }

        [BsonElement("cc")]
        public List<string> Cc { get; set; } = [];

        [BsonElement("bcc")]
        public List<string> Bcc { get; set; } = [];

        [BsonElement("subject")]
        public string Subject { get; set; }

        [BsonElement("body")]
        public string Body { get; set; }

        [BsonElement("processedAt")]
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("indexed")]
        public bool Indexed { get; set; } = true;  // Ensures we can track indexed emails
    }
}
