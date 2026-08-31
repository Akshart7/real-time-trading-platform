using Microsoft.AspNetCore.Mvc;
using RealTimeTradingPlatform.Api.Services;

namespace RealTimeTradingPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(
        IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestAuthentication(
        CancellationToken cancellationToken)
    {
        var token = await _authenticationService
            .AuthenticateAsync(cancellationToken);

        return Ok(new
        {
            authenticated = true
        });
    }
}