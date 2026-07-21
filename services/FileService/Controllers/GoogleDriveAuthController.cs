using FileService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

// NOTE: Auth is intentionally not wired up for this pass (matches FilesController) — endpoints are open.
[ApiController]
[Route("api/files/google")]
public class GoogleDriveAuthController : ControllerBase
{
    private readonly GoogleOAuthService _oauthService;
    public GoogleDriveAuthController(GoogleOAuthService oauthService) => _oauthService = oauthService;

    [HttpGet("auth-url")]
    public IActionResult GetAuthUrl() => Ok(new { authUrl = _oauthService.BuildAuthUrl() });

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] GoogleCallbackRequest req, CancellationToken ct)
    {
        await _oauthService.ExchangeCodeAndStoreAsync(req.Code, ct);
        var (connected, email) = await _oauthService.GetStatusAsync(ct);
        return Ok(new { connected, email });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var (connected, email) = await _oauthService.GetStatusAsync(ct);
        return Ok(new { connected, email });
    }
}

public record GoogleCallbackRequest(string Code);
