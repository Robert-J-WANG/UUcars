using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class CarUpdateRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(100, ErrorMessage = "Title must not exceed 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Brand is required.")]
    [MaxLength(50, ErrorMessage = "Brand must not exceed 50 characters.")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    [MaxLength(50, ErrorMessage = "Model must not exceed 50 characters.")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "Year is required.")]
    [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100.")]
    public int Year { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Mileage is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Mileage must be 0 or greater.")]
    public int Mileage { get; set; }

    [MaxLength(2000, ErrorMessage = "Description must not exceed 2000 characters.")]
    public string? Description { get; set; }
}