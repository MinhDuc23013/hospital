using DoctorScheduleService.Application.Queries;
using DoctorScheduleService.Infrastructure.Repositories;
using Elastic.Clients.Elasticsearch;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class ListDoctorsHandler : IRequestHandler<ListDoctorsQuery, (List<DoctorDto> Items, int Total)>
{
    private readonly IDoctorRepository _repo;
    private readonly ElasticsearchClient _esClient;
    private readonly ILogger<ListDoctorsHandler> _logger;

    private const string DoctorIndex = "hospital-doctors";

    public ListDoctorsHandler(IDoctorRepository repo, ElasticsearchClient esClient, ILogger<ListDoctorsHandler> logger)
    {
        _repo = repo;
        _esClient = esClient;
        _logger = logger;
    }

    public async Task<(List<DoctorDto> Items, int Total)> Handle(ListDoctorsQuery query, CancellationToken ct)
    {
        // Use Elasticsearch for full-text search (when searchName is provided)
        if (!string.IsNullOrEmpty(query.SearchName))
        {
            try
            {
                var from = (query.Page - 1) * query.PageSize;

                var response = await _esClient.SearchAsync<DoctorEsDoc>(s =>
                {
                    s.Index(DoctorIndex).From(from).Size(query.PageSize);

                    if (query.IsActive.HasValue)
                    {
                        s.Query(q => q.Bool(b => b
                            .Must(must => must.MultiMatch(mm => mm
                                .Query(query.SearchName)
                                .Fields(new[] { "fullName", "specialty", "email" })
                            ))
                            .Filter(f => f.Term(t => t.Field("isActive").Value(query.IsActive.Value)))
                        ));
                    }
                    else
                    {
                        s.Query(q => q.MultiMatch(mm => mm
                            .Query(query.SearchName)
                            .Fields(new[] { "fullName", "specialty", "email" })
                        ));
                    }
                }, ct);

                if (response.IsValidResponse)
                {
                    var total = response.HitsMetadata?.Total?.Value ?? 0;
                    var items = response.Documents.Select(d => new DoctorDto
                    {
                        Id = Guid.TryParse(d.DoctorId, out var id) ? id : Guid.Empty,
                        FullName = d.FullName,
                        Specialty = d.Specialty,
                        Phone = d.Phone,
                        Email = d.Email,
                        IsActive = d.IsActive
                    }).ToList();

                    return (items, (int)total);
                }

                _logger.LogWarning("ES search invalid response, falling back to DB");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ES unavailable, falling back to DB");
            }
        }

        // Fallback: direct DB query
        var (doctors, dbTotal) = await _repo.ListAsync(query.SearchName, query.Specialty, query.IsActive, query.Page, query.PageSize, ct);
        return (doctors.Select(DoctorMapper.ToDto).ToList(), dbTotal);
    }

    /// <summary>ES document model matching hospital-doctors index.</summary>
    private class DoctorEsDoc
    {
        public string DoctorId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
    }
}
