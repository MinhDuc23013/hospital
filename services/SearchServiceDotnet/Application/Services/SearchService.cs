using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using SearchServiceDotnet.Application.Models;

namespace SearchServiceDotnet.Application.Services;

/// <summary>
/// Provides multi-field search over Elasticsearch indexes for patients and drugs.
/// Uses from/size pagination and multi_match queries.
/// </summary>
public class SearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<SearchService> _logger;

    private const string PatientIndex = "hospital-patients";
    private const string DrugIndex    = "hospital-drugs";

    public SearchService(ElasticsearchClient client, ILogger<SearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Searches patients by multi-match on firstName, lastName, email.
    /// Returns documents and total count for pagination.
    /// </summary>
    public async Task<(IReadOnlyCollection<PatientDocument> Items, long Total)> SearchPatientsAsync(
        string query, int page, int pageSize, CancellationToken ct = default)
    {
        var from = (page - 1) * pageSize;

        var response = await _client.SearchAsync<PatientDocument>(s => s
            .Index(PatientIndex)
            .From(from)
            .Size(pageSize)
            .Query(q => q
                .MultiMatch(mm => mm
                    .Query(query)
                    .Fields(new[] { "firstName", "lastName", "email" })
                )
            ), ct);

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Patient search returned invalid response: {Debug}", response.DebugInformation);
            return (Array.Empty<PatientDocument>(), 0);
        }

        var items = response.Documents;
        var total = response.HitsMetadata?.Total?.Value ?? 0;
        return (items, total);
    }

    /// <summary>
    /// Searches drugs by multi-match on name and code fields.
    /// Returns documents and total count for pagination.
    /// </summary>
    public async Task<(IReadOnlyCollection<DrugDocument> Items, long Total)> SearchDrugsAsync(
        string query, int page, int pageSize, CancellationToken ct = default)
    {
        var from = (page - 1) * pageSize;

        var response = await _client.SearchAsync<DrugDocument>(s => s
            .Index(DrugIndex)
            .From(from)
            .Size(pageSize)
            .Query(q => q
                .MultiMatch(mm => mm
                    .Query(query)
                    .Fields(new[] { "name", "code" })
                )
            ), ct);

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Drug search returned invalid response: {Debug}", response.DebugInformation);
            return (Array.Empty<DrugDocument>(), 0);
        }

        var items = response.Documents;
        var total = response.HitsMetadata?.Total?.Value ?? 0;
        return (items, total);
    }
}
