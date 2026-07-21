using FileService.Application.Configuration;
using FileService.Domain.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace FileService.Application.Services;

/// <summary>
/// Converts uploaded files into Google-native formats (Docs/Sheets) via the org-wide OAuth
/// connection, so admins can open them in Google's own editor instead of previewing in-app.
/// </summary>
public class GoogleDriveEditService
{
    private readonly GoogleDriveOptions _options;
    private readonly GoogleOAuthService _oauthService;
    private readonly ILogger<GoogleDriveEditService> _logger;

    public GoogleDriveEditService(
        IOptions<GoogleDriveOptions> options, GoogleOAuthService oauthService, ILogger<GoogleDriveEditService> logger)
    {
        _options = options.Value;
        _oauthService = oauthService;
        _logger = logger;
    }

    /// <summary>Maps a source content-type to the Google-native mimeType Drive should convert it to.</summary>
    private static readonly Dictionary<string, string> ConvertibleContentTypes = new()
    {
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = "application/vnd.google-apps.document",
        ["text/plain"] = "application/vnd.google-apps.document",
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = "application/vnd.google-apps.spreadsheet",
        ["text/csv"] = "application/vnd.google-apps.spreadsheet",
    };

    /// <summary>
    /// Returns a cached edit link if one already exists for this file, otherwise uploads/converts
    /// the file to Google Drive and returns the newly created file's id + Drive edit URL.
    /// </summary>
    public async Task<(string DriveFileId, string EditUrl)> GetOrCreateEditLinkAsync(
        StoredFile metadata, byte[] content, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(metadata.DriveEditUrl))
            return (metadata.DriveFileId ?? string.Empty, metadata.DriveEditUrl);

        if (!ConvertibleContentTypes.TryGetValue(metadata.ContentType, out var targetMimeType))
            throw new NotSupportedException($"File type '{metadata.ContentType}' is not editable in Google Drive");

        var driveService = await GetDriveServiceAsync(ct);

        var driveFile = new Google.Apis.Drive.v3.Data.File
        {
            Name = metadata.FileName,
            Parents = new[] { _options.FolderId },
            MimeType = targetMimeType,
        };

        var request = driveService.Files.Create(driveFile, new MemoryStream(content), metadata.ContentType);
        request.Fields = "id, webViewLink";

        var uploadResult = await request.UploadAsync(ct);
        if (uploadResult.Status != Google.Apis.Upload.UploadStatus.Completed)
            throw new InvalidOperationException($"Google Drive upload failed: {uploadResult.Exception?.Message}");

        var created = request.ResponseBody
            ?? throw new InvalidOperationException("Google Drive upload did not return a response body.");

        if (string.IsNullOrWhiteSpace(created.Id) || string.IsNullOrWhiteSpace(created.WebViewLink))
            throw new InvalidOperationException("Google Drive did not return an id/webViewLink for the created file.");

        _logger.LogInformation("Converted file {FileId} to Google Drive file {DriveFileId}", metadata.Id, created.Id);

        return (created.Id, created.WebViewLink);
    }

    /// <summary>
    /// Exports a Google-native Drive file (Doc/Sheet) back to a target mimeType (usually the
    /// original upload's ContentType) — used by the "Sync from Drive" flow to pull edits back.
    /// </summary>
    public async Task<byte[]> ExportFileAsync(string driveFileId, string targetMimeType, CancellationToken ct)
    {
        var driveService = await GetDriveServiceAsync(ct);
        var request = driveService.Files.Export(driveFileId, targetMimeType);

        using var stream = new MemoryStream();
        var result = await request.DownloadAsync(stream, ct);
        if (result.Status != Google.Apis.Download.DownloadStatus.Completed)
            throw new InvalidOperationException($"Google Drive export failed: {result.Exception?.Message}");

        return stream.ToArray();
    }

    /// <summary>Deletes the Drive-side copy — called after a successful sync-back to Postgres,
    /// since the DB is the source of truth and the Drive copy was only a scratch pad for editing.</summary>
    public async Task DeleteFileAsync(string driveFileId, CancellationToken ct)
    {
        var driveService = await GetDriveServiceAsync(ct);
        await driveService.Files.Delete(driveFileId).ExecuteAsync(ct);
    }

    /// <summary>
    /// Builds a fresh DriveService per call using the current OAuth access token — access tokens
    /// expire and rotate (unlike the old static service-account credential), so no caching here.
    /// </summary>
    private async Task<DriveService> GetDriveServiceAsync(CancellationToken ct)
    {
        var accessToken = await _oauthService.GetValidAccessTokenAsync(ct);
        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "FileService",
        });
    }
}
