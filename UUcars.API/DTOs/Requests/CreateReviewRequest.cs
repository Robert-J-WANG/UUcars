using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class CreateReviewRequest
{
    [Required(ErrorMessage = "OrderId is required.")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Rating is required.")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [MaxLength(500, ErrorMessage = "Comment must not exceed 500 characters.")]
    public string? Comment { get; set; }
}