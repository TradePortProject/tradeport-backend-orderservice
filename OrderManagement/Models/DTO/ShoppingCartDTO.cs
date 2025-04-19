namespace OrderManagement.Models.DTO
{
    public class ShoppingCartDTO :CreateShoppingCartDTO
    {
        public Guid CartID { get; set; }
        public string ProductImagePath { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsOutOfStock { get; set; }
        public string RetailerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string? ProductName { get; set; }
        public int OrderQuantity { get; set; }
        public Guid ManufacturerID { get; set; }
        public decimal ProductPrice { get; set; }

    }
}
