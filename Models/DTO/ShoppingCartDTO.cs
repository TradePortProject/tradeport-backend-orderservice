namespace OrderManagement.Models.DTO
{
    public class ShoppingCartDTO :CreateShoppingCartDTO
    {
        public Guid CartID { get; set; }
        public string ProductImagePath { get; set; }
        public decimal TotalPrice { get; set; }
        public bool isOutOfStock { get; set; }
        public int NumberOfOrderItems { get; set; }

    }
}
