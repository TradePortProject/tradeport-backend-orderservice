using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Models.DTO
{
    public class CreateOrderDTO
    {
        public Guid RetailerID { get; set; }    
        public int PaymentMode { get; set; }
        public string PaymentCurrency { get; set; }
        public decimal ShippingCost { get; set; }
        public string ShippingCurrency { get; set; }
        public string ShippingAddress { get; set; }
        public Guid CreatedBy { get; set; }
        public List<CreateOrderDetailsDTO> OrderDetails { get; set; }
    }
}
