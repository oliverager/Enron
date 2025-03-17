using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SharedKernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;

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
                return BadRequest("Query cannot be empty.");

            try
            {
                var filter = Builders<Email>.Filter.Text(query);
                var results = _emailCollection.Find(filter).ToList();
                return Ok(results);
            }
            catch (MongoCommandException ex) when (ex.Message.Contains("text index required"))
            {
                return StatusCode(500, "⚠️ No text index found on MongoDB. Please ensure an index exists.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"🚨 An error occurred: {ex.Message}");
            }
        }
    }
}