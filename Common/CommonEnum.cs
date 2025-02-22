using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Common
{
    public enum OrderStatus
    {
        //Order Status (New, InProgress, Shipped, Delivered)
        [Display(Name = "New")]
        New = 1,
        [Display(Name = "In Progress")]
        InProgress = 2,
        [Display(Name = "Shipped")]
        Shipped = 3,
        [Display(Name = "Delivered")]
        Delivered = 4,       
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
