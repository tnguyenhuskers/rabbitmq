using System;
using System.Collections.Generic;

namespace NopOrderImporter.Models
{
    public class OrderLineItem
    {
        public int OrderLineItemId { get; set; }
        public string SKU { get; set; }
        public string UnitOfMeasure { get; set; }
        public string VariantCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineItemDiscountAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal SalesTax { get; set; }
        public List<KeyValuePair<string, string>> CustomAttributes { get; set; }
        public string ShippingSeason { get; set; }
        public bool EndOfSeason { get; set; }
        public bool Rebooked { get; set; }
        public bool ByPassTCO { get; set; }
        public int DesignId { get; set; }
        public DateTime EventDate { get; set; }
        public string Honoree { get; set; }
        public string Giver { get; set; }
        public string ForestName { get; set; }
        public int TreeCount { get; set; }
    }
}
