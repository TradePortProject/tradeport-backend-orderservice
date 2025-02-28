namespace OrderManagement.Models.DTO
{
    public class ProductDTO
    {
        public Guid ProductID { get; set; }
        public int? Quantity { get; set; } // Only fields required for stock updates
    }
}