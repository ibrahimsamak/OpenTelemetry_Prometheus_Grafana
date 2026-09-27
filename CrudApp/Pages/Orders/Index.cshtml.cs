using CrudApp.Data;
using CrudApp.Models;
using CrudApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace CrudApp.Pages.Orders;

public class IndexModel(AppDbContext db, OrderSimulator simulator) : PageModel
{
    public IList<Order> Orders { get; private set; } = [];

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        Orders = await db.Orders.AsNoTracking().OrderByDescending(o => o.Id).Take(200).ToListAsync();
    }

    public async Task<IActionResult> OnPostSimulateAsync(int count = 10)
    {
        var results = await simulator.RunAsync(count, HttpContext.RequestAborted);
        Message = "Simulated: " + string.Join(", ", results.Select(r => $"{r.Key}={r.Value}"));
        return RedirectToPage();
    }


}