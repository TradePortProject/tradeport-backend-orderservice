using System.ComponentModel.DataAnnotations;

public class User
{

    [Key]
    public Guid UserID { get; set; }

    [Required]
    [StringLength(255)]
    public string UserName { get; set; } = string.Empty;

    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [StringLength(20)]
    public string PhoneNo { get; set; } = string.Empty;

    [StringLength(255)]
    public string LoginID { get; set; } = string.Empty;

    public int Role { get; set; } // Optional, if needed
}
