namespace OrderManagement.Models
{
    public class Notification
    {
        public Guid NotificationID { get; set; }
        public Guid? UserID { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string FromEmail { get; set; }
        public string RecipientEmail { get; set; }
        public string FailureReason { get; set; }
        public DateTime? SentTime { get; set; }
        public bool? EmailSend { get; set; }
        public DateTime? CreatedOn { get; set; }
        public Guid? CreatedBy { get; set; }
    }
}
