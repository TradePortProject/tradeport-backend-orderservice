using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public Guid UserID { get; set; }

    [Required]
    [StringLength(255)]
    public string UserName { get; set; }

    public int Role { get; set; } // Optional, if needed
}
