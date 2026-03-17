namespace EstateReportingAPI.BusinessLogic;

using Merchant = EstateReportingAPI.Models.Merchant;

internal static class ModelFactory {
    internal static Merchant ConvertFrom(MerchantData merchant,
                                         decimal balance) {
        return new Merchant {
            Balance = balance,
            CreatedDateTime = merchant.CreatedDateTime,
            Name = merchant.Name,
            Reference = merchant.Reference,
            MerchantId = merchant.MerchantId,
            MerchantReportingId = merchant.MerchantReportingId,
            SettlementSchedule = merchant.SettlementSchedule,
            AddressId = merchant.AddressInfo.AddressId,
            AddressLine1 = merchant.AddressInfo.AddressLine1,
            AddressLine2 = merchant.AddressInfo.AddressLine2,
            Town = merchant.AddressInfo.Town,
            Region = merchant.AddressInfo.Region,
            PostCode = merchant.AddressInfo.PostalCode,
            Country = merchant.AddressInfo.Country,
            ContactId = merchant.ContactInfo.ContactId,
            ContactName = merchant.ContactInfo.Name,
            ContactEmail = merchant.ContactInfo.EmailAddress,
            ContactPhone = merchant.ContactInfo.PhoneNumber
        };
    }
}
