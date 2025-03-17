using SharedKernel.Models;
using EmailIndexer.Domain.Models;
using EmailIndexer.Infrastructure.Data;
using Email = SharedKernel.Models.Email;

namespace EmailIndexer.Infrastructure.Services
{
    public class EmailIndexerService
    {
        private readonly IndexerDbContext _context;

        public EmailIndexerService(IndexerDbContext context)
        {
            _context = context;
        }

        public void IndexEmail(Email email)
        {
            _context.Emails.Add(new EmailIndexer.Domain.Models.Email { Body = email.Body });
            _context.SaveChanges();
            Console.WriteLine($"✅ Indexed email: {email.Subject}");
        }
    }
}