using CrudApp.Data;
using CrudApp.Models;
using CrudApp.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrudApp.Pages.Orders;

public class CreateModel(AppDbContext db, ILogger<CreateModel> logger) : PageModel
{
    [BindProperty]
    public Order Order { get; set; } = new();

    public void OnGet()
    {
        // No-op
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        Order.Id = 0;
        Order.Status = OrderStatus.Pending;
        Order.CreatedAtUtc = DateTime.UtcNow;
        db.Orders.Add(Order);
        await db.SaveChangesAsync();

        AppTelemetry.OrderOperations.Add(1, new KeyValuePair<string, object?>("operation", "create"));
        logger.LogInformation("Order {OrderId} created for {CustomerName} ({Product} x{Quantity})",
            Order.Id, Order.CustomerName, Order.Product, Order.Quantity);

        return RedirectToPage("./Detail", new { id = Order.Id });
    }

}