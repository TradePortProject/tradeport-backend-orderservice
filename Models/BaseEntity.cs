namespace OrderManagement.Models
{
    public class BaseEntity
    {
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow; // Ensure this has a valid value
        public Guid CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; } = null;
        public Guid? UpdatedBy { get; set; }
    }
}
