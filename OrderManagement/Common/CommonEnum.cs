using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Common
{
    public enum OrderStatus
    {
        //Order Status (New, InProgress, Shipped, Delivered)
        [Display(Name = "Save")]
        Save = 1,
        [Display(Name = "Submitted")]
        Submitted = 2,
        [Display(Name = "Accepted")]
        Accepted = 3,
        [Display(Name = "Rejected")]
        Rejected = 4,
        [Display(Name = "In Progress")]
        InProgress = 5,
        [Display(Name = "Shipped")]
        Shipped = 6,
        [Display(Name = "Delivered")]
        Delivered = 7,       
    }

    public enum PaymentMode
    {
        [Display(Name = "Cash on Delivery")]
        Cash = 1,
        [Display(Name = "Credit Card")]
        CreditCard = 2,
        [Display(Name = "Debit Card")]
        DebitCard = 3,
        [Display(Name = "PayPal")]
        PayPal = 4     
    }
}
