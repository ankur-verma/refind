using Cortex.Database;
using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<CortexDbContext>()
    .UseNpgsql("Host=localhost;Database=cortex;Username=postgres;Password=postgres")
    .Options;

using var db = new CortexDbContext(options);
var count = db.ContentItems.Count();
var items = db.ContentItems.Include(x => x.Payload).ToList();
Console.WriteLine($"Total items: {count}");
foreach (var item in items)
{
    var embLength = item.Payload?.SemanticEmbedding?.ToArray().Length ?? 0;
    var embSum = item.Payload?.SemanticEmbedding?.ToArray().Sum() ?? 0;
    Console.WriteLine($"- {item.Title} (Embedding Length: {embLength}, Sum: {embSum})");
}
