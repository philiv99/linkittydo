using System.ComponentModel.DataAnnotations;
using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Tests;

public class InputValidationTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    // --- GuessRequest Tests ---

    [Fact]
    public void GuessRequest_ValidInput_PassesValidation()
    {
        var request = new GuessRequest { WordIndex = 1, Guess = "hello" };
        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GuessRequest_EmptyOrNullGuess_FailsValidation(string? guess)
    {
        var request = new GuessRequest { WordIndex = 1, Guess = guess! };
        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void GuessRequest_GuessTooLong_FailsValidation()
    {
        var request = new GuessRequest { WordIndex = 1, Guess = new string('a', 101) };
        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("hello; DROP TABLE")]
    [InlineData("test@#$%")]
    public void GuessRequest_InvalidCharacters_FailsValidation(string guess)
    {
        var request = new GuessRequest { WordIndex = 1, Guess = guess };
        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData("hello world")]
    [InlineData("it's")]
    [InlineData("well-known")]
    [InlineData("test123")]
    public void GuessRequest_AllowedSpecialCharacters_PassesValidation(string guess)
    {
        var request = new GuessRequest { WordIndex = 1, Guess = guess };
        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void GuessRequest_WordIndexOutOfRange_FailsValidation(int index)
    {
        var request = new GuessRequest { WordIndex = index, Guess = "test" };
        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    // --- StartGameRequest Tests ---

    [Fact]
    public void StartGameRequest_NullUserId_PassesValidation()
    {
        var request = new StartGameRequest { UserId = null, Difficulty = 10 };
        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Fact]
    public void StartGameRequest_ValidUserId_PassesValidation()
    {
        var request = new StartGameRequest { UserId = "USR-1736588400000-A1B2C3", Difficulty = 50 };
        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("invalid-id")]
    [InlineData("<script>")]
    [InlineData("USR-abc-XYZ")]
    public void StartGameRequest_InvalidUserIdFormat_FailsValidation(string userId)
    {
        var request = new StartGameRequest { UserId = userId, Difficulty = 10 };
        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void StartGameRequest_DifficultyOutOfRange_FailsValidation(int difficulty)
    {
        var request = new StartGameRequest { Difficulty = difficulty };
        var results = ValidateModel(request);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void StartGameRequest_DifficultyAtBoundaries_PassesValidation()
    {
        var request0 = new StartGameRequest { Difficulty = 0 };
        var request100 = new StartGameRequest { Difficulty = 100 };
        Assert.Empty(ValidateModel(request0));
        Assert.Empty(ValidateModel(request100));
    }
}
