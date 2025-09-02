using System;
using System.ComponentModel.DataAnnotations;

namespace TodoList.Web.Models
{
    public class TodoItemCreateViewModel
    {
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        [Display(Name = "Title")]
        public string ItemTitle { get; set; }

        [MaxLength(200)]
        [MinLength(15)]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Content")]
        public string ItemContent { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Due to date")]
        public DateTime? DueToDateTime { get; set; }

        [MaxLength(100)]
        [Display(Name = "Tags")]
        public string ItemTags { get; set; }
    }
}