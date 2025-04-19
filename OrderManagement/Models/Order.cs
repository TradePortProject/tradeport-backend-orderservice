using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Models
{
    public class Order :BaseEntity
    {

        [Key]
        public Guid OrderID { get; set; }

        [Required]
        public Guid RetailerID { get; set; }


        public Guid? DeliveryPersonnelID { get; set; }

        [Required]
        public int OrderStatus { get; set; }


        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [Required] 
        public int PaymentMode { get; set; }

        [Required]
        [StringLength(3)]
        public string PaymentCurrency { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ShippingCost { get; set; }

        [Required]
        [StringLength(3)]
        public string ShippingCurrency { get; set; }

        [StringLength(500)]
        public string ShippingAddress { get; set; }

        // Add this navigation property for OrderDetails
        public ICollection<OrderDetails> OrderDetails { get; set; } = new List<OrderDetails>();

    }
}

