using CrudApp.Data;
using CrudApp.Models;
using CrudApp.Telemetry;

namespace CrudApp.Services;

public class OrderSimulator(AppDbContext db, OrderSaga saga, ILogger<OrderSimulator> logger)
{
    private static readonly string[] Customers = ["Alice", "Bob", "Carol", "Dave", "Eve", "Frank"];
    private static readonly string[] Products = ["Laptop", "Phone", "Monitor", "Keyboard", "Headphones"];

    public async Task<Dictionary<string, int>> RunAsync(int count, CancellationToken ct = default)
    {
        count = Math.Clamp(count, 1, 500);
        var results = new Dictionary<string, int>();

        for (var i = 0; i < count; i++)
        {
            var order = new Order
            {
                CustomerName = Customers[Random.Shared.Next(Customers.Length)],
                Product = Products[Random.Shared.Next(Products.Length)],
                Quantity = Random.Shared.Next(1, 5),
                Amount = Math.Round((decimal)(Random.Shared.NextDouble() * 990 + 10), 2)
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            AppTelemetry.OrderOperations.Add(1, new KeyValuePair<string, object?>("operation", "create"));

            var outcome = (await saga.RunAsync(order, ct)).ToString().ToLowerInvariant();
            results[outcome] = results.GetValueOrDefault(outcome) + 1;
        }

        logger.LogInformation("Simulated {Count} orders: {Completed} completed, {Compensated} compensated, {Failed} failed",
            count,
            results.GetValueOrDefault("completed"),
            results.GetValueOrDefault("compensated"),
            results.GetValueOrDefault("failed"));

        return results;
    }
}
