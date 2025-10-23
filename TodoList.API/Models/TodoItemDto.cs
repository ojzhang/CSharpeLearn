using System;
using System.ComponentModel.DataAnnotations;

namespace TodoList.API.Models
{
    public class TodoItemDto
    {
        [Required]
        [MinLength(1)]
        [MaxLength(50)]
        public string? Title { get; set; }

        [MinLength(1)]
        [MaxLength(200)]
        public string? Content { get; set; }

        public bool? Done { get; set; }

        public DateTime DuetoDateTime { get; set; }

        // Allow Unicode letters/numbers (e.g. Chinese) as well as ASCII word characters and hyphen.
        // Limit to at most 3 comma-separated tags.
        [RegularExpression(@"^(?:[\p{L}\p{N}_\-]*,?){0,3}$", ErrorMessage = "Maximum 3 comma separated tags!")]
        public string? Tags { get; set; }
    }
}
