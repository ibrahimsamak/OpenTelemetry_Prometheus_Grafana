using CrudApp.Data;
using CrudApp.Models;
using CrudApp.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrudApp.Pages.Orders;

public class EditModel(AppDbContext db, ILogger<EditModel> logger) : PageModel
{
    public Order Order { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        Order = order;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        // Only these fields may be changed from the form (prevents over-posting Status/CreatedAtUtc).
        if (!await TryUpdateModelAsync(order, "Order",
                o => o.CustomerName, o => o.Product, o => o.Quantity, o => o.Amount))
        {
            Order = order;
            return Page();
        }

        await db.SaveChangesAsync();

        AppTelemetry.OrderOperations.Add(1, new KeyValuePair<string, object?>("operation", "update"));
        logger.LogInformation("Order {OrderId} updated", order.Id);

        return RedirectToPage("./Index");
    }
}
