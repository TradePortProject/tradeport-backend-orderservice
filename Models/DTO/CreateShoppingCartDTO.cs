namespace OrderManagement.Models.DTO
{
    public class CreateShoppingCartDTO
    {
        public Guid ProductID { get; set; }
        public Guid RetailerID { get; set; }
        public Guid ManufacturerID { get; set; }
        public int OrderQuantity { get; set; }
        public decimal ProductPrice { get; set; }



    }
}
