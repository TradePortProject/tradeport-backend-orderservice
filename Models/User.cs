using System.ComponentModel.DataAnnotations;

public class User
{

    [Key]
    public Guid UserID { get; set; }

    [Required]
    [StringLength(255)]
    public string UserName { get; set; }

    [StringLength(500)]
    public string Address { get; set; }

    [StringLength(20)]
    public string PhoneNo { get; set; }

    public int Role { get; set; } // Optional, if needed
}
