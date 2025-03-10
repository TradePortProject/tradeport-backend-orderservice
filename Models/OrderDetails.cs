using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Models
{
    public class OrderDetails :BaseEntity
    {
        [Key]
        public Guid OrderDetailID { get; set; } // Primary key
       
        [Required]
        public Guid OrderID { get; set; } // Foreign key to Order

        [ForeignKey("OrderID")]
        public Order Order { get; set; } // Navigation property

        [Required]
        public Guid ProductID { get; set; }


        [Required]
        public Guid ManufacturerID { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public int OrderItemStatus { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ProductPrice { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow; // Ensure a valid default

        public Guid CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; } = null; 

        public Guid? UpdatedBy { get; set; }

    }
}

