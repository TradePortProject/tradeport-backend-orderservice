

namespace OrderManagement.Models.DTO
{
    public class GetOrderDetailsDTO
    {
        public Guid OrderDetailID { get; set; }
        public Guid OrderID { get; set; }
        public Guid ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal ProductPrice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}