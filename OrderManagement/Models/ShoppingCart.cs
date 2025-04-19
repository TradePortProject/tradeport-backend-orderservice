using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Models
{
    public class ShoppingCart : BaseEntity
    {
        [Key]
        public Guid CartID { get; set; } // Primary key   

        [Required]
        public Guid ProductID { get; set; }

        [Required]
        public int Status { get; set; }

        [Required]
        public Guid RetailerID { get; set; }

        [Required]
        public int OrderQuantity { get; set; }

        [Required]
        public Guid ManufacturerID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ProductPrice { get; set; }

    }
}