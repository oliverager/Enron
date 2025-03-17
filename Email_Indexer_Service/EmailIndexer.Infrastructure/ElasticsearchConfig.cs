using Nest;

namespace EmailIndexer.Infrastructure
{
    public static class ElasticsearchConfig
    {
        public static IElasticClient CreateClient()
        {
            var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
                .DefaultIndex("emails");

            return new ElasticClient(settings);
        }
    }
}