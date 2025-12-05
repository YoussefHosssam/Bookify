using System.ComponentModel.DataAnnotations;

namespace Bookify.Core.ViewModels;

public class RoomFeedbackViewModel
{
    [Required]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Please provide your feedback")]
    [StringLength(1000, ErrorMessage = "Feedback must be less than 1000 characters")]
    public string Comment { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide a rating")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; } = 5;
}

