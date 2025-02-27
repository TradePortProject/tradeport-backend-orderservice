
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Models.DTO
{
    public class GetOrderDTO
    {
        public Guid OrderID { get; set; }
        public Guid RetailerID { get; set; }
        public Guid? ManufacturerID { get; set; }
        public Guid? DeliveryPersonnelID { get; set; }
        public string OrderStatus { get; set; }
        public bool IsActive { get; set; }
        public decimal TotalPrice { get; set; }
        public int PaymentMode { get; set; }
        public string PaymentCurrency { get; set; }
        public decimal ShippingCost { get; set; }
        public string ShippingCurrency { get; set; }
        public string ShippingAddress { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}

