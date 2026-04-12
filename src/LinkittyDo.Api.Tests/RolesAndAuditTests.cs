using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace LinkittyDo.Api.Tests;

public class RolesAndAuditTests
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
    public void Role_SeedData_HasThreeRoles()
    {
        using var context = CreateInMemoryContext();
        var roles = context.Roles.ToList();
        Assert.Equal(3, roles.Count);
        Assert.Contains(roles, r => r.Name == "Player");
        Assert.Contains(roles, r => r.Name == "Moderator");
        Assert.Contains(roles, r => r.Name == "Admin");
    }

    [Fact]
    public async Task RoleService_AssignRole_CreatesUserRole()
    {
        using var context = CreateInMemoryContext();
        context.Users.Add(new User
        {
            UniqueId = "USR-1234567890123-A1B2C3",
            Name = "TestUser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new RoleService(context);
        await service.AssignRoleAsync("USR-1234567890123-A1B2C3", "Admin");

        var roles = await service.GetUserRolesAsync("USR-1234567890123-A1B2C3");
        Assert.Single(roles);
        Assert.Equal("Admin", roles[0]);
    }

    [Fact]
    public async Task RoleService_AssignDuplicateRole_DoesNotDuplicate()
    {
        using var context = CreateInMemoryContext();
        context.Users.Add(new User
        {
            UniqueId = "USR-1234567890123-NODUP1",
            Name = "NoDupe",
            Email = "nodupe@example.com",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new RoleService(context);
        await service.AssignRoleAsync("USR-1234567890123-NODUP1", "Player");
        await service.AssignRoleAsync("USR-1234567890123-NODUP1", "Player");

        var roles = await service.GetUserRolesAsync("USR-1234567890123-NODUP1");
        Assert.Single(roles);
    }

    [Fact]
    public async Task RoleService_RemoveRole_DeletesUserRole()
    {
        using var context = CreateInMemoryContext();
        context.Users.Add(new User
        {
            UniqueId = "USR-1234567890123-REMROL",
            Name = "RemRole",
            Email = "remrole@example.com",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new RoleService(context);
        await service.AssignRoleAsync("USR-1234567890123-REMROL", "Moderator");
        await service.RemoveRoleAsync("USR-1234567890123-REMROL", "Moderator");

        var roles = await service.GetUserRolesAsync("USR-1234567890123-REMROL");
        Assert.Empty(roles);
    }

    [Fact]
    public async Task RoleService_GetRoles_ForUserWithNoRoles_ReturnsEmpty()
    {
        using var context = CreateInMemoryContext();
        var service = new RoleService(context);
        var roles = await service.GetUserRolesAsync("USR-NONEXISTENT-123456");
        Assert.Empty(roles);
    }

    [Fact]
    public async Task RoleClaimsTransformation_AddsRoleClaims()
    {
        using var context = CreateInMemoryContext();
        context.Users.Add(new User
        {
            UniqueId = "USR-1234567890123-CLAIMS",
            Name = "ClaimUser",
            Email = "claim@example.com",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var roleService = new RoleService(context);
        await roleService.AssignRoleAsync("USR-1234567890123-CLAIMS", "Admin");

        var transformer = new RoleClaimsTransformation(roleService);
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "USR-1234567890123-CLAIMS")
        }, "test");
        var principal = new ClaimsPrincipal(identity);

        var result = await transformer.TransformAsync(principal);
        Assert.True(result.IsInRole("Admin"));
    }

    [Fact]
    public void AuditLogEntry_HasCorrectDefaults()
    {
        var entry = new AuditLogEntry();
        Assert.Equal(0, entry.Id);
        Assert.Null(entry.UserId);
        Assert.Equal(string.Empty, entry.Action);
    }

    [Fact]
    public async Task AuditLog_CanStoreAndRetrieve()
    {
        using var context = CreateInMemoryContext();
        context.AuditLog.Add(new AuditLogEntry
        {
            UserId = "USR-1234567890123-AUDIT1",
            Action = "UserCreated",
            EntityType = "User",
            EntityId = "USR-1234567890123-AUDIT1",
            Details = "{\"name\": \"test\"}",
            IpAddress = "127.0.0.1",
            Timestamp = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var entries = await context.AuditLog.Where(a => a.Action == "UserCreated").ToListAsync();
        Assert.Single(entries);
        Assert.Equal("User", entries[0].EntityType);
    }

}
