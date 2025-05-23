﻿using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Models
{
    public class Product
    {
        [Key]
        public Guid ProductID { get; set; }

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; }

        public Guid ManufacturerID { get; set; } // Optional: If you need Manufacturer Mapping

        public int? Quantity { get; set; }

        public DateTime UpdatedOn { get; set; }

        public bool IsActive { get; set; }
    }
}
