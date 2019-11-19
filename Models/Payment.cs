using System;

namespace NopOrderImporter.Models
{
    public class Payment
    {
        public decimal Amount { get; set; }
        public DateTime ChargeDateUTC { get; set; }
        public DateTime ExpirationDate { get; set; }
        public PaymentType PaymentType { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public string CreditCardToken { get; set; }
    }
}
