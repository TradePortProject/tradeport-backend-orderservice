namespace OrderManagement.Models.DTO
{
    public class CreateOrderDetailsDTO
    {
        public Guid ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal ProductPrice { get; set; }
    }
}
