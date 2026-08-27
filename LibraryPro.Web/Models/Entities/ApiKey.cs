using System.ComponentModel.DataAnnotations;

namespace LibraryPro.Web.Models.Entities;

public class ApiKey
{
    [Key]
    public int Id { get; set; }
    
    [Required, StringLength(100)]
    public string Key { get; set; } = string.Empty;
    
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Owner { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastUsedAt { get; set; }
    
    public int UsageCount { get; set; } = 0;
}
