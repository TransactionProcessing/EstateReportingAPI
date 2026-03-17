namespace EstateReportingAPI.BusinessLogic;

using System;

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
