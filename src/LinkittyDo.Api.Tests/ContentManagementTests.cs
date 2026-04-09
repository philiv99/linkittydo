using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class ContentManagementTests
{
    private static LinkittyDoDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LinkittyDoDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void SiteConfig_SeedData_ContainsDefaults()
    {
        using var context = CreateInMemoryContext();
        var configs = context.SiteConfigs.ToList();
        Assert.True(configs.Count >= 4);
        Assert.Contains(configs, c => c.Key == "MaxSessionTtlHours" && c.Value == "24");
        Assert.Contains(configs, c => c.Key == "DefaultDifficulty" && c.Value == "10");
        Assert.Contains(configs, c => c.Key == "MaintenanceMode" && c.Value == "false");
    }

    [Fact]
    public void PhraseCategory_SeedData_HasSixCategories()
    {
        using var context = CreateInMemoryContext();
        var categories = context.PhraseCategories.ToList();
        Assert.Equal(6, categories.Count);
        Assert.Contains(categories, c => c.Name == "Idioms");
        Assert.Contains(categories, c => c.Name == "Proverbs");
        Assert.Contains(categories, c => c.Name == "Pop Culture");
        Assert.Contains(categories, c => c.Name == "Science");
    }

    [Fact]
    public async Task PhraseCategoryAssignment_CanAssignCategoryToPhrase()
    {
        using var context = CreateInMemoryContext();
        context.GamePhrases.Add(new GamePhrase
        {
            UniqueId = "PHR-1234567890123-CATEG1",
            Text = "Test phrase for category",
            WordCount = 4,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        context.PhraseCategoryAssignments.Add(new PhraseCategoryAssignment
        {
            PhraseUniqueId = "PHR-1234567890123-CATEG1",
            CategoryId = 1 // Idioms
        });
        await context.SaveChangesAsync();

        var assignment = await context.PhraseCategoryAssignments
            .FirstOrDefaultAsync(a => a.PhraseUniqueId == "PHR-1234567890123-CATEG1");
        Assert.NotNull(assignment);
        Assert.Equal(1, assignment.CategoryId);
    }

    [Fact]
    public async Task PhraseReview_CanCreateAndQuery()
    {
        using var context = CreateInMemoryContext();
        context.GamePhrases.Add(new GamePhrase
        {
            UniqueId = "PHR-1234567890123-REVW01",
            Text = "Test phrase for review",
            WordCount = 4,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        context.PhraseReviews.Add(new PhraseReview
        {
            PhraseUniqueId = "PHR-1234567890123-REVW01",
            SubmittedBy = "USR-1234567890123-A1B2C3",
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var pendingReviews = await context.PhraseReviews
            .Where(r => r.Status == "Pending")
            .ToListAsync();
        Assert.Single(pendingReviews);
    }

    [Fact]
    public async Task PhraseReview_CanApprove()
    {
        using var context = CreateInMemoryContext();
        context.GamePhrases.Add(new GamePhrase
        {
            UniqueId = "PHR-1234567890123-APPRVD",
            Text = "Approved phrase test",
            WordCount = 3,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        context.PhraseReviews.Add(new PhraseReview
        {
            PhraseUniqueId = "PHR-1234567890123-APPRVD",
            SubmittedBy = "USR-1234567890123-SUB001",
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var review = await context.PhraseReviews.FirstAsync(r => r.PhraseUniqueId == "PHR-1234567890123-APPRVD");
        review.Status = "Approved";
        review.ReviewedBy = "USR-1234567890123-ADMIN1";
        review.ReviewedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var approved = await context.PhraseReviews.FindAsync(review.Id);
        Assert.Equal("Approved", approved!.Status);
    }

    [Fact]
    public void InMemorySiteConfigService_BasicOperations()
    {
        var service = new InMemorySiteConfigService();

        var defaultDiff = service.GetIntAsync("DefaultDifficulty", 0).Result;
        Assert.Equal(10, defaultDiff);

        var maintenance = service.GetBoolAsync("MaintenanceMode", true).Result;
        Assert.False(maintenance);

        service.SetValueAsync("TestKey", "TestValue").Wait();
        var val = service.GetValueAsync("TestKey").Result;
        Assert.Equal("TestValue", val);
    }

    [Fact]
    public async Task InMemorySiteConfigService_GetAll_ReturnsAllConfigs()
    {
        var service = new InMemorySiteConfigService();
        var configs = await service.GetAllAsync();
        Assert.True(configs.Count >= 4);
    }

    [Fact]
    public void SiteConfig_Model_HasCorrectDefaults()
    {
        var config = new SiteConfig();
        Assert.Equal(string.Empty, config.Key);
        Assert.Equal(string.Empty, config.Value);
        Assert.Equal("string", config.ValueType);
        Assert.Null(config.Description);
    }

    [Fact]
    public void PhraseCategory_Model_HasCorrectDefaults()
    {
        var category = new PhraseCategory();
        Assert.True(category.IsActive);
        Assert.Equal(string.Empty, category.Name);
    }

    [Fact]
    public void PhraseReview_Model_HasCorrectDefaults()
    {
        var review = new PhraseReview();
        Assert.Equal("Pending", review.Status);
        Assert.Null(review.ReviewedBy);
        Assert.Null(review.ReviewedAt);
    }
}
