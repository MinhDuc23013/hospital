using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace SearchServiceDotnet.Infrastructure.Elasticsearch;

/// <summary>
/// Creates and configures the ElasticsearchClient singleton from appsettings.
/// Reads Uri from Elasticsearch:Uri config key.
/// </summary>
public static class ElasticsearchClientFactory
{
    public static ElasticsearchClient Create(IConfiguration configuration)
    {
        var uri = configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";

        var settings = new ElasticsearchClientSettings(new Uri(uri))
            .EnableDebugMode()
            .PrettyJson();

        return new ElasticsearchClient(settings);
    }
}
