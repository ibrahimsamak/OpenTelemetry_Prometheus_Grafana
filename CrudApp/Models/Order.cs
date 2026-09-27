using System.ComponentModel.DataAnnotations;

namespace CrudApp.Models;

public enum OrderStatus
{
    Pending,
    Completed,
    Compensated,
    Failed
}

public class Order
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Customer")]
    public string CustomerName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Product { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [Range(typeof(decimal), "0.01", "1000000")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Display(Name = "Created (UTC)")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
