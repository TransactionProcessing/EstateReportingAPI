namespace EstateReportingAPI.BusinessLogic;

using System;
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

internal sealed class MerchantData {
    public Guid MerchantId { get; init; }
    public int MerchantReportingId { get; init; }
    public string? Name { get; init; }
    public DateTime CreatedDateTime { get; init; }
    public string? Reference { get; init; }
    public int SettlementSchedule { get; init; }
    public MerchantAddressData? AddressInfo { get; init; }
    public MerchantContactData? ContactInfo { get; init; }
}

internal sealed class MerchantAddressData {
    public Guid AddressId { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? Country { get; init; }
    public string? PostalCode { get; init; }
    public string? Region { get; init; }
    public string? Town { get; init; }
}

internal sealed class MerchantContactData {
    public Guid ContactId { get; init; }
    public string? Name { get; init; }
    public string? EmailAddress { get; init; }
    public string? PhoneNumber { get; init; }
}
