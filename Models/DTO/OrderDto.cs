namespace OrderManagement.Models.DTO
{
    public class OrderDto
    {
        public Guid OrderID { get; set; }
        public Guid RetailerID { get; set; }
        public Guid? DeliveryPersonnelID { get; set; }
        public int OrderStatus { get; set; }
        public decimal TotalPrice { get; set; }
        public int PaymentMode { get; set; }
        public string PaymentCurrency { get; set; }
        public decimal ShippingCost { get; set; }
        public string ShippingCurrency { get; set; }
        public string ShippingAddress { get; set; }
        public List<OrderDetailsDto> OrderDetails { get; set; }
    }
}
