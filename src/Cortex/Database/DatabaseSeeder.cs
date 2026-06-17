using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cortex.Database;

public class DatabaseSeeder
{
    private readonly CortexDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(CortexDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();

            await SeedTagsAsync();
            // Subscriptions and other master data can be seeded here

            await _context.SaveChangesAsync();
            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedTagsAsync()
    {
        if (await _context.Tags.AnyAsync())
        {
            return;
        }

        var defaultTags = new[]
        {
            new Tag { Name = "technology", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "productivity", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "health", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "finance", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "inspiration", CreatedAt = DateTime.UtcNow },
            new Tag { Name = "news", CreatedAt = DateTime.UtcNow }
        };

        await _context.Tags.AddRangeAsync(defaultTags);
        _logger.LogInformation("Seeded default tags.");
    }
}
