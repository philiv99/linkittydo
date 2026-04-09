using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigrationController : ControllerBase
{
    private readonly IDataMigrationService _migrationService;
    private readonly ILogger<MigrationController> _logger;

    public MigrationController(IDataMigrationService migrationService, ILogger<MigrationController> logger)
    {
        _migrationService = migrationService;
        _logger = logger;
    }

    /// <summary>
    /// Migrates JSON file data to MySQL database. Idempotent — skips existing records.
    /// </summary>
    [HttpPost("json-to-mysql")]
    [Authorize]
    public async Task<IActionResult> MigrateJsonToMySql(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JSON to MySQL migration requested");
        var result = await _migrationService.MigrateAsync(cancellationToken);

        return Ok(new
        {
            data = result,
            message = result.Success ? "Migration completed successfully" : "Migration completed with errors"
        });
    }
}
