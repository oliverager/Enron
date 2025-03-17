using System.Collections.Generic;
using Nest;
using EmailIndexer.Domain.Models;

namespace EmailIndexer.Infrastructure.Services
{
    public class EmailSearchService
    {
        private readonly IElasticClient _elasticClient;

        public EmailSearchService(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public List<Email> SearchEmails(string query)
        {
            var response = _elasticClient.Search<Email>(s => s
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Body)
                        .Query(query)
                    )
                )
            );

            return response.Documents.ToList();
        }
    }
}