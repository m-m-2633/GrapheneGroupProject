using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneSensore.Models
{
    public class Section
    {
        [Key]
        public Guid SectionId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string SectionName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }
    }
}
