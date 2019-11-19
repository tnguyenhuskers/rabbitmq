using System.Collections.Generic;

namespace NopOrderImporter.Models
{
    public class Customer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string StateCode { get; set; }
        public string PostalCode { get; set; }
        public string CountryCode { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        public string AdfCustomerNumber { get; set; }
        public List<string> Roles { get; set; }
    }
}
