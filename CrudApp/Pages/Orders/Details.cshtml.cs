using CrudApp.Data;
using CrudApp.Models;
using CrudApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CrudApp.Pages.Orders;

public class DetailsModel(AppDbContext db, OrderSaga saga) : PageModel
{
    public Order Order { get; set; } = default!;

    [TempData]
    public string? Message { get; set; }

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

    public async Task<IActionResult> OnPostRunSagaAsync(int id)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Pending)
        {
            Message = $"Order is already {order.Status}; the saga only runs for Pending orders.";
            return RedirectToPage(new { id });
        }

        var outcome = await saga.RunAsync(order, HttpContext.RequestAborted);
        Message = $"Saga finished: {outcome}";
        return RedirectToPage(new { id });
    }
}
