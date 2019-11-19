using System.Linq;
using BaileyParty = Bailey.Core.CustomerManagment.Party;
using BaileyAddress = Bailey.Core.CustomerManagment.Address;
using BaileyCommunicationMethod = Bailey.Core.CustomerManagment.CommunicationMethod;
using BaileyTransaction = Bailey.Core.BailyTracking.Transaction;
using BaileyPaymentRequest = Bailey.Core.PaymentManagement.PaymentRequest;
using BaileyOnAccount = Bailey.Core.PaymentManagement.OnAccount;
using BaileyItem = Bailey.Core.OrderManagement.Item;
using BaileyOrder = Bailey.Core.OrderManagement.Order;
using Bailey.Core;
using NopOrderImporter.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace NopOrderImporter
{
    public class NopToBaileyTransformer
    {
        private readonly string _baileyConnectionString;

        public NopToBaileyTransformer(IOptions<WorkerOptions> options)
        {
            //_baileyConnectionString = options.Value.BaileyConnectionString;
            _baileyConnectionString = "data source=LNKDB4T;initial catalog=Bailey_Tracking;Integrated Security=True";
        }

        public async Task<Boolean> LoadDataIntoBailey(Order order)
        {
            var baily = new BailyBase("BaileyTracking", _baileyConnectionString); //TODO: Inject this???
            var baileyTransaction = GetBaileyTransaction(order);
            return baily.AddNewTransaction("ZnodeToNav", baileyTransaction);
        }

        public async Task<Boolean>  LoadCustomerIntoBailey(Order order)
        {
            var baily = new BailyBase("BaileyTracking", _baileyConnectionString); //TODO: Inject this???
            var baileyTransaction = GetCustomerTransaction(order);
            return baily.AddNewTransaction("ZnodeToNav", baileyTransaction);
        }

       

         private BaileyTransaction GetCustomerTransaction(Order order)
        {
            var billToParty = GetBillToParty(order);
            var shipToParty = GetShipToParty(order);

            return new BaileyTransaction
            {
                Parties =
                {
                    billToParty,
                    shipToParty
                },
                Orders = null,
                SourceTracking =
                {
                    System =
                    {
                        SystemName = "WEB",
                        SystemNumber = order.BillingCustomer.EmailAddress
                    }
                },
                PaymentRequests = null
                
            };
        }

         private BaileyParty GetCustomerParty(Customer customer)
         {
             var party = new BaileyParty
             {
                 FirstName = customer.FirstName,
                 LastName = customer.LastName,
                 Company = customer.CompanyName,
                 ADFCustomerNumber = customer.AdfCustomerNumber,
                 CustomerPostingGroup = GetPostingGroupForCustomer(customer),
                 Addresses =
                 {
                     new BaileyAddress
                     {
                         AddressLine1 = customer.Address1,
                         AddressLine2 = customer.Address2,
                         City = customer.City,
                         State = customer.StateCode,
                         PostalCode = customer.PostalCode,
                         Country = customer.CountryCode
                     }
                 },
                 CommunicationMethods =
                 {
                     new BaileyCommunicationMethod
                     {
                         LocationName = "",
                         PhoneNumber = customer.PhoneNumber,
                         MobilePhoneNumber = "",
                         EmailAddress = customer.EmailAddress
                     }
                 }
             };

             party.SourceTracking.System.SystemName = "Nop";
             party.SourceTracking.System.SystemNumber = "";

             return party;
         }
         private BaileyParty GetBillToParty(Order order)
         {
             var party = new BaileyParty
             {
                 FirstName = order.BillingCustomer.FirstName,
                 LastName = order.BillingCustomer.LastName,
                 Company = order.BillingCustomer.CompanyName,
                 ADFCustomerNumber = order.BillingCustomer.AdfCustomerNumber,
                 CustomerPostingGroup = GetPostingGroupForCustomer(order.BillingCustomer),
                 Addresses =
                 {
                     new BaileyAddress
                     {
                         AddressLine1 = order.BillingCustomer.Address1,
                         AddressLine2 = order.BillingCustomer.Address2,
                         City = order.BillingCustomer.City,
                         State = order.BillingCustomer.StateCode,
                         PostalCode = order.BillingCustomer.PostalCode,
                         Country = order.BillingCustomer.CountryCode
                     }
                 },
                 CommunicationMethods =
                 {
                     new BaileyCommunicationMethod
                     {
                         LocationName = "",
                         PhoneNumber = order.BillingCustomer.PhoneNumber,
                         MobilePhoneNumber = "",
                         EmailAddress = order.BillingCustomer.EmailAddress
                     }
                 }
             };

             party.SourceTracking.System.SystemName = "Nop";
             party.SourceTracking.System.SystemNumber = "";

             return party;
         }
        private BaileyParty GetShipToParty(Order order)
        {
            var party = new BaileyParty
            {
                FirstName = order.ShippingCustomer.FirstName,
                LastName = order.ShippingCustomer.LastName,
                Company = order.ShippingCustomer.CompanyName,
                ADFCustomerNumber = order.ShippingCustomer.AdfCustomerNumber,
                CustomerPostingGroup = GetPostingGroupForCustomer(order.ShippingCustomer),
                Addresses =
                {
                    new BaileyAddress
                    {
                        AddressLine1 = order.ShippingCustomer.Address1,
                        AddressLine2 = order.ShippingCustomer.Address2,
                        City = order.ShippingCustomer.City,
                        State = order.ShippingCustomer.StateCode,
                        PostalCode = order.ShippingCustomer.PostalCode,
                        Country = order.ShippingCustomer.CountryCode
                    }
                },
                CommunicationMethods =
                {
                    new BaileyCommunicationMethod
                    {
                        LocationName = "",
                        PhoneNumber = order.ShippingCustomer.PhoneNumber,
                        MobilePhoneNumber = "",
                        EmailAddress = order.ShippingCustomer.EmailAddress
                    }
                }
            };

            party.SourceTracking.System.SystemName = "Nop";
            party.SourceTracking.System.SystemNumber = "";

            return party;
        }

        private string GetPostingGroupForCustomer(Customer customer)
        {
            if (customer.Roles.Contains("Animal Professional", StringComparer.OrdinalIgnoreCase))
            {
                return "PT";
            }
            else if (customer.Roles.Contains("Corporate Partner", StringComparer.OrdinalIgnoreCase))
            {
                return "CP";
            }
            else
            {
                return "AR";
            }
        }

        private BaileyTransaction GetBaileyTransaction(Order order)
        {
            var billToParty = GetBillToParty(order);
            var shipToParty = GetShipToParty(order);

            var paymentRequest = new BaileyPaymentRequest();
            if (order.PaymentRequest.PaymentType == PaymentType.CreditCard)
            {
                paymentRequest = new BaileyPaymentRequest
                {
                    Invoice =
                    {
                        BillToCustomer = billToParty.UniqueID,
                        TotalAmount = (double) order.Total
                    },
                    Tender = new
                    {
                        BillToCustomer = billToParty.UniqueID,
                        AuthorizationRefNum = order.PaymentRequest.CreditCardToken,
                        ApprovedAmount = order.PaymentRequest.Amount,
                        ChargeDate = order.PaymentRequest.ChargeDateUTC,
                        ExpirationDate = order.PaymentRequest.ExpirationDate
                    }
                };
            }

            if (order.PaymentRequest.PaymentType == PaymentType.OnAccount)
            {
                paymentRequest = new BaileyPaymentRequest
                {
                    Invoice =
                    {
                        BillToCustomer = billToParty.UniqueID,
                        TotalAmount = (double) order.Total
                    },

                    Tender = new BaileyOnAccount
                    {
                        BillToCustomer = billToParty.UniqueID,
                    }
                };
            }

            return new BaileyTransaction
            {
                Parties =
                {
                    billToParty,
                    shipToParty
                },
                Orders =
                {
                    GetBaileyOrder(order, billToParty, shipToParty, paymentRequest)
                },
                SourceTracking =
                {
                    System =
                    {
                        SystemName = "WEB",
                        SystemNumber = order.OrderNumber
                    }
                },
                PaymentRequests =
                {
                    paymentRequest
                }
            };
        }

        private static BaileyOrder GetBaileyOrder(Order order, BaileyParty billToParty,
            BaileyParty shipToParty, BaileyPaymentRequest paymentRequest)
        {
            var baileyOrder = new BaileyOrder
            {
                BillToCustomer = billToParty.UniqueID,
                SellToCustomer = billToParty.UniqueID,
                OrderDate = order.OrderDateUTC,
                SourceTracking =
                {
                    System =
                    {
                        SystemName = "WEB",
                        SystemNumber = order.OrderNumber
                    }
                },
                SourceInformation =
                {
                    ProjectCode = "",
                    StringCode = ""
                },
                Items = order
                    .OrderLineItems
                    .Select(line => GetBaileyOrderItem(line, billToParty, shipToParty, paymentRequest))
                    .ToList(),
            };

            return baileyOrder;
        }

        private static BaileyItem GetBaileyOrderItem(OrderLineItem line, BaileyParty billToParty, BaileyParty shipToParty,
            BaileyPaymentRequest paymentRequest)
        {
            return new BaileyItem
            {
                Quantity = line.Quantity.ToString(),
                SellToCustomer = billToParty.UniqueID,
                BillToCustomer = billToParty.UniqueID,
                Shipment =
                {
                    ShipToCustomer = shipToParty.UniqueID
                },
                ItemID =
                {
                    SourcePartID = line.SKU,
                    UnitOfMeasure = line.UnitOfMeasure,
                    VariantCode = line.VariantCode
                },
                Charge =
                {
                    UnitPrice = (double) line.UnitPrice,
                    SalesTax = (double) line.SalesTax,
                    ShippingCharge = (double) line.ShippingCost
                },
                PaymentRequest = paymentRequest.UniqueID,
                SourceInformation =
                {
                    ProjectCode = "",
                    StringCode = ""
                }
            };
        }
    }
}
