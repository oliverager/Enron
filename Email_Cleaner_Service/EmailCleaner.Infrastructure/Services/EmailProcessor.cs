using System;
using System.Collections.Generic;
using System.IO;
using SharedKernel.Models;
using EmailCleaner.Infrastructure.Messaging;
using Serilog;

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
            Log.Information("📥 Starting email processing at {Time}", DateTime.UtcNow);
            
            if (!Directory.Exists(rootDirectory))
            {
                Log.Error("❌ Root directory not found: {Directory}", rootDirectory);
                return;
            }
            
            int processedCount = 0, skippedFiles = 0;
            
            foreach (var userDir in Directory.EnumerateDirectories(rootDirectory))
            {
                foreach (var mailFolder in Directory.EnumerateDirectories(userDir))
                {
                    foreach (var file in Directory.EnumerateFiles(mailFolder, "*.txt", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            var email = ParseAndPublishEmail(file);
                            if (email != null)
                            {
                                processedCount++;
                                Log.Information("✅ Email processed: {Subject} - From: {From}", email.Subject, email.From);
                            }
                            else
                            {
                                skippedFiles++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "🚨 Error processing file {File}", file);
                        }
                    }
                }
            }
            Log.Information("✅ Processing completed. Total: {Processed}, Skipped: {Skipped}", processedCount, skippedFiles);
        }
        
        private Email ParseAndPublishEmail(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    Log.Warning($"⚠️ Empty file: {Path.GetFileName(filePath)}");
                    return null;
                }
                
                var email = ParseEmail(content);
                
                if (email == null)
                {
                    Log.Warning($"⚠️ Failed to parse email: {Path.GetFileName(filePath)}");
                    return null;
                }
                
                _publisher.Publish(email);
                return email;
            }
            catch (Exception ex)
            {
                Log.Error($"🚨 Error processing {Path.GetFileName(filePath)}: {ex.Message}");
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

                // Remove parentheses and timezone abbreviation (e.g., "(PDT)")
                dateString = System.Text.RegularExpressions.Regex.Replace(dateString, @"\s*\(.*?\)", "").Trim();

                string[] possibleFormats = {
                    "ddd, d MMM yyyy HH:mm:ss zzz",  // Fri, 30 Jun 2000 06:41:00 -0700
                    "ddd, dd MMM yyyy HH:mm:ss zzz" // Fri, 30 Jun 2000 06:41:00 -0700
                };

                if (DateTime.TryParseExact(dateString, possibleFormats, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AllowWhiteSpaces, out DateTime parsedDate))
                {
                    email.Date = parsedDate.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    email.Date = "Invalid Date";
                    Log.Warning($"      ⚠️ Failed to parse date: {dateString}");
                }
            }

            else if (line.StartsWith("From: "))
            {
                email.From = line[6..].Trim();
            }
            else if (line.StartsWith("To: ")) 
                email.To = line[4..].Trim().Split(',').Select(x => x.Trim()).ToList();

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
        Log.Error($"🚨 Error parsing email: {ex.Message}");
        return null;
    }
}

    }
}