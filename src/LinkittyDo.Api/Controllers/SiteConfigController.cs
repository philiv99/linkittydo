using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/admin/config")]
[Authorize(Policy = "RequireAdmin")]
public class SiteConfigController : ControllerBase
{
    private readonly ISiteConfigService _configService;

    public SiteConfigController(ISiteConfigService configService)
    {
        _configService = configService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllConfigs()
    {
        var configs = await _configService.GetAllAsync();
        return Ok(new { data = configs });
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetConfig(string key)
    {
        var value = await _configService.GetValueAsync(key);
        if (value == null)
            return NotFound(new { error = new { code = "CONFIG_NOT_FOUND", message = $"Config key '{key}' not found" } });

        return Ok(new { data = new { key, value } });
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> SetConfig(string key, [FromBody] SetConfigRequest request)
    {
        await _configService.SetValueAsync(key, request.Value);
        return Ok(new { data = new { key, value = request.Value }, message = "Configuration updated" });
    }
}

public class SetConfigRequest
{
    public string Value { get; set; } = string.Empty;
}
