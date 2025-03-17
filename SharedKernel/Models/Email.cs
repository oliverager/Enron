namespace SharedKernel.Models
{
    public class Email
    {
        public string MessageId { get; set; }
        public string Date { get; set; } = "";
        public string From { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Bcc { get; set; }
        public string Subject { get; set; }
        public string FolderPath { get; set; }
        public string Origin { get; set; }
        public string Body { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
