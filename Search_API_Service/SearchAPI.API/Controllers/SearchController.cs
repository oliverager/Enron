using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using SharedKernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SearchAPI.API.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly IMongoCollection<Email> _emailCollection;

        public SearchController(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("EmailDB");
            _emailCollection = database.GetCollection<Email>("emails");
        }

        [HttpGet]
        public IActionResult SearchEmails([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("⚠️ Query cannot be empty.");
                return BadRequest("Query cannot be empty.");
            }

            try
            {
                var filters = new List<FilterDefinition<Email>>();
                var filterBuilder = Builders<Email>.Filter;

                // 🔍 Extract email addresses
                var emailMatches = Regex.Matches(query, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
                var emails = emailMatches.Select(m => m.Value).Distinct().ToList();

                if (emails.Any())
                    Console.WriteLine($"📧 Found Emails: {string.Join(", ", emails)}");

                // 🔍 Extract dates (supports multiple formats)
                var dateMatches = Regex.Matches(query, @"\b\d{4}-\d{2}-\d{2}\b|\b\d{2}/\d{2}/\d{4}\b|\b\w{3,9} \d{1,2}, \d{4}\b");
                var dates = dateMatches.Select(m => m.Value).Distinct().ToList();

                if (dates.Any())
                    Console.WriteLine($"📅 Found Dates: {string.Join(", ", dates)}");

                // 🔍 Remove extracted emails and dates from the query
                var cleanedQuery = query;
                foreach (var match in emails.Concat(dates))
                {
                    cleanedQuery = cleanedQuery.Replace(match, "").Trim();
                }

                // 🔍 Extract remaining keywords
                var keywords = cleanedQuery.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                                           .Select(k => k.Trim(','))
                                           .Distinct()
                                           .ToList();

                if (keywords.Any())
                    Console.WriteLine($"🔍 Found Keywords: {string.Join(", ", keywords)}");

                // ✅ Build MongoDB Filters

                // Filter by Email Addresses
                if (emails.Any())
                    filters.Add(filterBuilder.In(e => e.From, emails) | filterBuilder.AnyIn(e => e.To, emails));

                // Filter by Dates
                if (dates.Any())
                    filters.Add(filterBuilder.In(e => e.Date, dates));

                // ✅ Check if text index exists for keyword search
                var indexCheck = _emailCollection.Indexes.List().ToList();
                bool hasTextIndex = indexCheck.Any(idx => idx["name"] == "TextIndex");

                if (hasTextIndex && keywords.Any())
                {
                    Console.WriteLine("✅ Using MongoDB Text Search for keywords.");
                    filters.Add(filterBuilder.Text(string.Join(" ", keywords)));
                }
                else if (keywords.Any())
                {
                    // Fallback to regex for keywords if text index is unavailable
                    Console.WriteLine("⚠️ No text index found. Using regex fallback.");
                    filters.Add(filterBuilder.Or(
                        filterBuilder.Regex(e => e.Subject, new BsonRegularExpression(string.Join("|", keywords), "i")),
                        filterBuilder.Regex(e => e.Body, new BsonRegularExpression(string.Join("|", keywords), "i"))
                    ));
                }

                // Combine filters safely
                var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

                // Execute query
                var results = _emailCollection.Find(finalFilter).ToList();

                Console.WriteLine($"✅ Search executed. Found {results.Count} matching emails.");
                return Ok(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Error during search: {ex.Message}");
                return StatusCode(500, $"🚨 An error occurred: {ex.Message}");
            }
        }
    }
}
