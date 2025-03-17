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
            Console.WriteLine($"=== STARTED EMAIL PROCESSING: {DateTime.Now} ===");
            
            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"❌ Root directory not found: {rootDirectory}");
                return;
            }
            
            Console.WriteLine($"📂 Processing emails in: {rootDirectory}");
            
            var processedCount = 0;
            var skippedFiles = 0;
            
            foreach (var userDir in Directory.EnumerateDirectories(rootDirectory))
            {
                Console.WriteLine($"👤 User directory: {Path.GetFileName(userDir)}");
                
                foreach (var mailFolder in Directory.EnumerateDirectories(userDir))
                {
                    Console.WriteLine($"  📁 Mail folder: {Path.GetFileName(mailFolder)}");
                    
                    foreach (var file in Directory.EnumerateFiles(mailFolder, "*.txt", SearchOption.TopDirectoryOnly))
                    {
                        if (!File.Exists(file)) continue; // Skip if file doesn't exist
                        
                        Console.WriteLine($"    📄 Processing file: {Path.GetFileName(file)}");
                        var email = ParseAndPublishEmail(file);
                        
                        if (email == null)
                        {
                            skippedFiles++;
                            Console.WriteLine($"      ❌ Skipped file. Total skipped: {skippedFiles}");
                            
                            if (skippedFiles >= 10)
                            {
                                Console.WriteLine("❌ Too many skipped files, stopping.");
                                return;
                            }
                        }
                        else
                        {
                            processedCount++;
                            Console.WriteLine($"      ✅ EMAIL #{processedCount} PROCESSED");
                            Console.WriteLine($"      📧 ID: {email.MessageId}");
                            Console.WriteLine($"      📅 Date: {email.Date}");
                            Console.WriteLine($"      👤 From: {email.From}");
                            Console.WriteLine($"      👥 To: {email.To}");
                            Console.WriteLine($"      📝 Subject: {email.Subject}");
                            Console.WriteLine($"      📄 Body: {(email.Body.Length > 50 ? email.Body.Substring(0, 50) + "..." : email.Body)}");
                            Console.WriteLine($"      🕒 Processed at: {email.ProcessedAt}");
                            Console.WriteLine("      ------------------------------");
                        }
                    }
                }
            }
            
            Console.WriteLine($"=== COMPLETED EMAIL PROCESSING: {DateTime.Now} ===");
            Console.WriteLine($"✅ Total processed: {processedCount}");
            Console.WriteLine($"❌ Total skipped: {skippedFiles}");
        }
        
        private Email ParseAndPublishEmail(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine($"      ⚠️ Empty file: {Path.GetFileName(filePath)}");
                    return null;
                }
                
                var email = ParseEmail(content);
                
                if (email == null)
                {
                    Console.WriteLine($"      ⚠️ Failed to parse email: {Path.GetFileName(filePath)}");
                    return null;
                }

                 Console.WriteLine(email);
                
                _publisher.Publish(email);
                return email;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      🚨 Error processing {Path.GetFileName(filePath)}: {ex.Message}");
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
            
            if (isBody)
            { 
                bodyLines.Add(line);
                continue;
            }
            
            if (line.StartsWith("Message-ID: "))
            {
                email.MessageId = line[11..].Trim();
            }
            else if (line.StartsWith("Date: "))
            {
                string dateString = line[6..].Trim();

                // Remove the time zone abbreviation part (e.g., "(PDT)")
                int timeZoneIndex = dateString.IndexOf('(');
                if (timeZoneIndex >= 0)
                {
                    dateString = dateString.Substring(0, timeZoneIndex).Trim();
                }

                // Try parsing the date with the expected format
                string[] dateFormats = {
                    "ddd, d MMM yyyy HH:mm:ss zzz", // Example: Wed, 9 May 2001 17:13:00 -0700
                    "ddd, dd MMM yyyy HH:mm:ss zzz" // Example: Wed, 09 May 2001 17:13:00 -0700
                };

                if (DateTime.TryParseExact(dateString, dateFormats, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    email.Date = parsedDate.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    email.Date = "Invalid Date";
                    Console.WriteLine($"      ⚠️ Invalid date format: {dateString}");
                }
            }
            else if (line.StartsWith("From: "))
            {
                email.From = line[6..].Trim();
            }
            else if (line.StartsWith("To: "))
            {
                email.To = line[4..].Trim();
            }
            else if (line.StartsWith("Subject: "))
            {
                email.Subject = line[9..].Trim();
            }
        }
        
        email.Body = string.Join("\n", bodyLines);
        email.ProcessedAt = DateTime.UtcNow;
        
        return email;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"      🚨 Error parsing email: {ex.Message}");
        return null;
    }
}

    }
}