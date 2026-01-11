using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "VALIDATION_ERROR",
                    Message = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))
                }
            });
        }

        // Check name availability
        if (!await _userService.IsNameAvailableAsync(request.Name))
        {
            return Conflict(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "NAME_TAKEN",
                    Message = "This name is already taken. Please choose a different name."
                }
            });
        }

        // Check email availability
        if (!await _userService.IsEmailAvailableAsync(request.Email))
        {
            return Conflict(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "EMAIL_TAKEN",
                    Message = "This email is already registered. Please use a different email."
                }
            });
        }

        try
        {
            var user = await _userService.CreateUserAsync(request);
            var response = MapToResponse(user);
            return CreatedAtAction(nameof(GetUser), new { uniqueId = user.UniqueId }, response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = ex.Message,
                    Message = ex.Message switch
                    {
                        "NAME_TAKEN" => "This name is already taken. Please choose a different name.",
                        "EMAIL_TAKEN" => "This email is already registered. Please use a different email.",
                        _ => "An error occurred while creating the user."
                    }
                }
            });
        }
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users.Select(MapToResponse));
    }

    /// <summary>
    /// Get a user by unique ID
    /// </summary>
    [HttpGet("{uniqueId}")]
    public async Task<ActionResult<UserResponse>> GetUser(string uniqueId)
    {
        var user = await _userService.GetUserByIdAsync(uniqueId);
        if (user == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "USER_NOT_FOUND",
                    Message = "User not found."
                }
            });
        }

        return Ok(MapToResponse(user));
    }

    /// <summary>
    /// Get a user by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<UserResponse>> GetUserByEmail(string email)
    {
        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "USER_NOT_FOUND",
                    Message = "User not found."
                }
            });
        }

        return Ok(MapToResponse(user));
    }

    /// <summary>
    /// Update a user
    /// </summary>
    [HttpPut("{uniqueId}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(string uniqueId, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "VALIDATION_ERROR",
                    Message = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))
                }
            });
        }

        // Check if user exists
        var existingUser = await _userService.GetUserByIdAsync(uniqueId);
        if (existingUser == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "USER_NOT_FOUND",
                    Message = "User not found."
                }
            });
        }

        // Check name availability (excluding current user)
        if (!await _userService.IsNameAvailableAsync(request.Name, uniqueId))
        {
            return Conflict(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "NAME_TAKEN",
                    Message = "This name is already taken. Please choose a different name."
                }
            });
        }

        // Check email availability (excluding current user)
        if (!await _userService.IsEmailAvailableAsync(request.Email, uniqueId))
        {
            return Conflict(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "EMAIL_TAKEN",
                    Message = "This email is already registered. Please use a different email."
                }
            });
        }

        try
        {
            var user = await _userService.UpdateUserAsync(uniqueId, request);
            return Ok(MapToResponse(user!));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = ex.Message,
                    Message = ex.Message switch
                    {
                        "NAME_TAKEN" => "This name is already taken. Please choose a different name.",
                        "EMAIL_TAKEN" => "This email is already registered. Please use a different email.",
                        _ => "An error occurred while updating the user."
                    }
                }
            });
        }
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{uniqueId}")]
    public async Task<IActionResult> DeleteUser(string uniqueId)
    {
        var deleted = await _userService.DeleteUserAsync(uniqueId);
        if (!deleted)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "USER_NOT_FOUND",
                    Message = "User not found."
                }
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Check if a name is available
    /// </summary>
    [HttpGet("check-name/{name}")]
    public async Task<ActionResult<AvailabilityResponse>> CheckNameAvailability(string name)
    {
        var available = await _userService.IsNameAvailableAsync(name);
        return Ok(new AvailabilityResponse { Available = available });
    }

    /// <summary>
    /// Check if an email is available
    /// </summary>
    [HttpGet("check-email/{email}")]
    public async Task<ActionResult<AvailabilityResponse>> CheckEmailAvailability(string email)
    {
        var available = await _userService.IsEmailAvailableAsync(email);
        return Ok(new AvailabilityResponse { Available = available });
    }

    /// <summary>
    /// Update user's preferred difficulty
    /// </summary>
    [HttpPatch("{uniqueId}/difficulty")]
    public async Task<ActionResult<DifficultyResponse>> UpdateDifficulty(string uniqueId, [FromBody] UpdateDifficultyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "VALIDATION_ERROR",
                    Message = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))
                }
            });
        }

        try
        {
            var user = await _userService.UpdateDifficultyAsync(uniqueId, request.Difficulty);
            if (user == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "USER_NOT_FOUND",
                        Message = "User not found."
                    }
                });
            }

            return Ok(new DifficultyResponse
            {
                UniqueId = user.UniqueId,
                PreferredDifficulty = user.PreferredDifficulty
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "INVALID_DIFFICULTY",
                    Message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Add points to user's lifetime total
    /// </summary>
    [HttpPost("{uniqueId}/points")]
    public async Task<ActionResult<PointsResponse>> AddPoints(string uniqueId, [FromBody] AddPointsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "VALIDATION_ERROR",
                    Message = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))
                }
            });
        }

        try
        {
            var user = await _userService.AddPointsAsync(uniqueId, request.Points);
            if (user == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "USER_NOT_FOUND",
                        Message = "User not found."
                    }
                });
            }

            return Ok(new PointsResponse
            {
                UniqueId = user.UniqueId,
                LifetimePoints = user.LifetimePoints,
                PointsAdded = request.Points
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "INVALID_POINTS",
                    Message = ex.Message
                }
            });
        }
    }

    /// <summary>
    /// Get a user's game history
    /// </summary>
    [HttpGet("{uniqueId}/games")]
    public async Task<ActionResult<IEnumerable<GameRecord>>> GetUserGames(string uniqueId)
    {
        var user = await _userService.GetUserByIdAsync(uniqueId);
        if (user == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "USER_NOT_FOUND",
                    Message = "User not found."
                }
            });
        }

        var games = await _userService.GetUserGamesAsync(uniqueId);
        return Ok(games);
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            UniqueId = user.UniqueId,
            Name = user.Name,
            Email = user.Email,
            LifetimePoints = user.LifetimePoints,
            PreferredDifficulty = user.PreferredDifficulty,
            GamesPlayed = user.Games.Count,
            CreatedAt = user.CreatedAt
        };
    }
}
