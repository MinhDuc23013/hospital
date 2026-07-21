using FileService.Domain.Entities;
using FileService.Domain.Exceptions;
using FileService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FileService.Infrastructure.Repositories;

public class GoogleDriveConnectionRepository : IGoogleDriveConnectionRepository
{
    private readonly FileDbContext _context;
    public GoogleDriveConnectionRepository(FileDbContext context) => _context = context;

    public Task<GoogleDriveConnection?> GetAsync(CancellationToken ct = default)
        => _context.DriveConnections.FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(string accessToken, string refreshToken, DateTime expiresAt, string connectedEmail, CancellationToken ct = default)
    {
        var existing = await _context.DriveConnections.FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            await _context.DriveConnections.AddAsync(
                GoogleDriveConnection.Create(accessToken, refreshToken, expiresAt, connectedEmail), ct);
        }
        else
        {
            existing.ReconsentUpdate(accessToken, expiresAt, refreshToken, connectedEmail);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAccessTokenAsync(string accessToken, DateTime expiresAt, CancellationToken ct = default)
    {
        var existing = await _context.DriveConnections.FirstOrDefaultAsync(ct)
            ?? throw new DriveNotConnectedException();

        existing.UpdateToken(accessToken, expiresAt);
        await _context.SaveChangesAsync(ct);
    }
}
