using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrchestratorService.Infrastructure.Persistence;

/// <summary>
/// Used only by EF Core CLI (dotnet ef migrations add/update).
/// Not referenced at runtime.
/// </summary>
public class OrchestratorDbContextFactory : IDesignTimeDbContextFactory<OrchestratorDbContext>
{
    public OrchestratorDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=hospital_db;Username=hospital;Password=dev_password_change_me")
            .Options;
        return new OrchestratorDbContext(options);
    }
}
