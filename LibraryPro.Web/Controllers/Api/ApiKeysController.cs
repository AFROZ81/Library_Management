using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(IApiKeyRepository apiKeyRepository, ILogger<ApiKeysController> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all API keys
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiKey>>> GetApiKeys()
    {
        try
        {
            var apiKeys = await _apiKeyRepository.GetAllAsync();
            return Ok(apiKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API keys");
            return StatusCode(500, new { error = "An error occurred while retrieving API keys" });
        }
    }

    /// <summary>
    /// Get an API key by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiKey>> GetApiKey(int id)
    {
        try
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(id);
            if (apiKey == null)
            {
                return NotFound(new { error = "API key not found" });
            }
            return Ok(apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API key with ID {ApiKey}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the API key" });
        }
    }

    /// <summary>
    /// Create a new API key
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiKey>> CreateApiKey([FromBody] CreateApiKeyDto createApiKeyDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var apiKey = new ApiKey
            {
                Key = GenerateApiKey(),
                Name = createApiKeyDto.Name,
                Owner = createApiKeyDto.Owner,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = createApiKeyDto.ExpiresAt,
                IsActive = true,
                UsageCount = 0
            };

            await _apiKeyRepository.AddAsync(apiKey);
            return CreatedAtAction(nameof(GetApiKey), new { id = apiKey.Id }, apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return StatusCode(500, new { error = "An error occurred while creating the API key" });
        }
    }

    /// <summary>
    /// Deactivate an API key
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeactivateApiKey(int id)
    {
        try
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(id);
            if (apiKey == null)
            {
                return NotFound(new { error = "API key not found" });
            }

            apiKey.IsActive = false;
            await _apiKeyRepository.UpdateAsync(apiKey);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating API key with ID {ApiKey}", id);
            return StatusCode(500, new { error = "An error occurred while deactivating the API key" });
        }
    }

    private string GenerateApiKey()
    {
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }
}

public class CreateApiKeyDto
{
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
