using System.ComponentModel.DataAnnotations;

namespace RealTimeTradingPlatform.Api.DTOs;

public class OrderRequestDto
{
    [Required]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    public string Side { get; set; } = string.Empty;

    [Range(0.000001, double.MaxValue)]
    public decimal Quantity { get; set; }
}