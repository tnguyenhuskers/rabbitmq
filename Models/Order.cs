using System;
using System.Collections.Generic;

namespace NopOrderImporter.Models
{
    public class Order
    {
        public string OrderNumber { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderDateUTC { get; set; }
        public string OrderStatus { get; set; }
        public Customer ShippingCustomer { get; set; }
        public Customer BillingCustomer { get; set; }
        public List<OrderLineItem> OrderLineItems { get; set; }
        public Payment PaymentRequest { get; set; }
    }
}
