using EmailIndexer.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EmailIndexer.API.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly IndexerDbContext _context;

        public SearchController(IndexerDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SearchEmails([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query cannot be empty.");

            var results = await _context.Emails
                .Where(e => EF.Functions.ILike(e.Body, $"%{query}%") || EF.Functions.ILike(e.Subject, $"%{query}%"))
                .ToListAsync();

            return Ok(results);
        }
    }
}