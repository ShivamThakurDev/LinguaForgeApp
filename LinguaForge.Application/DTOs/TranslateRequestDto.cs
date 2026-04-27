using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinguaForge.Application.DTOs
{
    public class TranslateRequestDto
    {
        [Required(ErrorMessage = "Text is required")]
        [MaxLength(5000, ErrorMessage = "Text cannot exceed 5000 characters")]
        public string Text { get; set; } = string.Empty;

        // Empty string = auto-detect via Azure
        public string From { get; set; } = string.Empty;

        [Required(ErrorMessage = "Target language is required")]
        public string To { get; set; } = string.Empty;
    }
}
