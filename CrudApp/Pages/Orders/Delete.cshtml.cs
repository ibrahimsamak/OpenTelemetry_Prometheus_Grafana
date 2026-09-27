using CrudApp.Data;
using CrudApp.Models;
using CrudApp.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CrudApp.Pages.Orders;

public class DeleteModel(AppDbContext db, ILogger<DeleteModel> logger) : PageModel
{
    public Order Order { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
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
        if (order is not null)
        {
            db.Orders.Remove(order);
            await db.SaveChangesAsync();

            AppTelemetry.OrderOperations.Add(1, new KeyValuePair<string, object?>("operation", "delete"));
            logger.LogInformation("Order {OrderId} deleted", id);
        }

        return RedirectToPage("./Index");
    }
}
