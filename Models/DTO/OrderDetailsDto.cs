namespace OrderManagement.Models.DTO
{
    public class OrderDetailsDto
    {
        public Guid OrderDetailID { get; set; }
        public Guid ProductID { get; set; }
        public Guid ManufacturerID { get; set; }
        public int Quantity { get; set; }
        public int OrderItemStatus { get; set; }
        public decimal ProductPrice { get; set; }
    }
}
