namespace OrderManagement.Models.DTO
{
    public class AcceptOrderDTO
    {
        public Guid OrderID { get; set; }
        public List<AcceptOrderItemDTO> OrderItems { get; set; } // ✅ Accept items at item level
    }

    public class AcceptOrderItemDTO
    {
        public Guid OrderDetailID { get; set; }
        public bool IsAccepted { get; set; } // ✅ True = Accepted, False = Rejected
    }
}
