namespace OrderManagement.Models.DTO
{
    public class AcceptOrderDTO
    {
        public Guid OrderID { get; set; }
        public string OrderStatus { get; set; } // Accepts status as string
        public string DeliveryPersonnelID { get; set; } // Can be empty or null
    }
}
