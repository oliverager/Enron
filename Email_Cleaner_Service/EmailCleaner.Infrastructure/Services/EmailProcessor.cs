using System;
using System.Collections.Generic;
using System.IO;
using SharedKernel.Models;
using EmailCleaner.Infrastructure.Messaging;

namespace EmailCleaner.Infrastructure.Services
{
    public class EmailProcessor
    {
        private readonly EasyNetQPublisher _publisher;

        public EmailProcessor(EasyNetQPublisher publisher)
        {
            _publisher = publisher;
        }

        public void ProcessEmails(string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"❌ Root directory not found: {rootDirectory}");
                return;
            }

            Console.WriteLine($"📂 Processing emails in: {rootDirectory}");

            var skippedFiles = 0;

            foreach (var userDir in Directory.EnumerateDirectories(rootDirectory))
            {
                foreach (var mailFolder in Directory.EnumerateDirectories(userDir))
                {
                    foreach (var file in Directory.EnumerateFiles(mailFolder, "*.txt", SearchOption.TopDirectoryOnly))
                    {
                        if (!File.Exists(file)) continue; // Skip if file doesn't exist

                        var email = ParseAndPublishEmail(file);
                        if (email == null)
                        {
                            skippedFiles++;
                            if (skippedFiles >= 10)
                            {
                                Console.WriteLine("❌ Too many skipped files, stopping.");
                                return;
                            }
                        }
                    }
                }
            }
        }

        private Email ParseAndPublishEmail(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(content)) return null;

                var email = ParseEmail(content);
                if (email == null) return null;

                _publisher.Publish(email);
                return email;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Error processing {filePath}: {ex.Message}");
                return null;
            }
        }

        private Email ParseEmail(string rawContent)
        {
            try
            {
                var lines = rawContent.Split("\n");
                var email = new Email();
                var isBody = false;
                var bodyLines = new List<string>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) { isBody = true; continue; }
                    if (isBody) { bodyLines.Add(line); continue; }

                    if (line.StartsWith("Message-ID: ")) email.MessageId = line[11..].Trim();
                    else if (line.StartsWith("Date: "))
                    {
                        string dateString = line[6..].Trim();
                        if (DateTime.TryParse(dateString, out DateTime parsedDate))
                            email.Date = parsedDate.ToString("yyyy-MM-dd HH:mm:ss");
                        else
                            email.Date = "Invalid Date";
                    }
                    else if (line.StartsWith("From: ")) email.From = line[6..].Trim();
                    else if (line.StartsWith("To: ")) email.To = line[4..].Trim();
                    else if (line.StartsWith("Subject: ")) email.Subject = line[9..].Trim();
                }

                email.Body = string.Join("\n", bodyLines);
                email.ProcessedAt = DateTime.UtcNow;

                return email;
            }
            catch
            {
                return null;
            }
        }
    }
}
