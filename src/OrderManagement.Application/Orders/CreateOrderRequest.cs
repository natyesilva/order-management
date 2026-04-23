using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.Orders;

public sealed class CreateOrderRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(200)]
    public string Customer { get; set; } = string.Empty;

    [Required]
    [MinLength(2)]
    [MaxLength(200)]
    public string Product { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Value { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;
}
