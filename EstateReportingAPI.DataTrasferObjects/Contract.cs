using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing contract information.</summary>

public class Contract
{
    #region Properties
    /// <summary>Unique identifier of the estate.</summary>
    public Guid EstateId { get; set; }
    /// <summary>The estate reporting id value.</summary>
    public Int32 EstateReportingId { get; set; }
    /// <summary>Unique identifier of the contract.</summary>
    public Guid ContractId { get; set; }
    /// <summary>The contract reporting id value.</summary>
    public Int32 ContractReportingId { get; set; }
    /// <summary>The description value.</summary>
    public String Description { get; set; }
    /// <summary>The operator name value.</summary>
    public String OperatorName { get; set; }
    /// <summary>Unique identifier of the operator.</summary>
    public Guid OperatorId { get; set; }
    /// <summary>The operator reporting id value.</summary>
    public Int32 OperatorReportingId { get; set; }
    /// <summary>Optional product reporting identifiers to include.</summary>
    public List<ContractProduct> Products { get; set; }
    #endregion
}

/// <summary>API data model describing contract product information.</summary>

public class ContractProduct
{
    /// <summary>Unique identifier of the contract.</summary>
    public Guid ContractId { get; set; }
    /// <summary>The contract product reporting id value.</summary>
    public Int32 ContractProductReportingId { get; set; }

    /// <summary>Unique identifier of the product.</summary>

    public Guid ProductId { get; set; }
    /// <summary>The product name value.</summary>
    public String ProductName { get; set; }
    /// <summary>The display text value.</summary>
    public String DisplayText { get; set; }
    /// <summary>The product type value.</summary>
    public Int32 ProductType { get; set; }
    /// <summary>The value value.</summary>
    public Decimal? Value { get; set; }
    /// <summary>The transaction fees value.</summary>
    public List<ContractProductTransactionFee> TransactionFees { get; set; }
}

/// <summary>API data model describing contract product transaction fee information.</summary>

public class ContractProductTransactionFee {
    /// <summary>The transaction fee id value.</summary>
    public Guid TransactionFeeId { get; set; }
    /// <summary>The contract product transaction fee reporting id value.</summary>
    public Int32 ContractProductTransactionFeeReportingId { get; set; }

    /// <summary>The description value.</summary>

    public string? Description { get; set; }
    /// <summary>The calculation type value.</summary>
    public Int32 CalculationType { get; set; }
    /// <summary>The fee type value.</summary>
    public Int32 FeeType { get; set; }
    /// <summary>The value value.</summary>
    public Decimal Value { get; set; }
}


/// <summary>API data model describing estate information.</summary>


public class Estate
{
    /// <summary>Unique identifier of the estate.</summary>
    public Guid EstateId { get; set; }
    /// <summary>The estate name value.</summary>
    public string? EstateName { get; set; }
    /// <summary>The reference value.</summary>
    public string? Reference { get; set; }
    /// <summary>Optional operator reporting identifiers to include.</summary>
    public List<EstateOperator>? Operators { get; set; }
    /// <summary>Optional merchant reporting identifiers to include.</summary>
    public List<EstateMerchant>? Merchants { get; set; }
    /// <summary>The contracts value.</summary>
    public List<EstateContract>? Contracts { get; set; }
    /// <summary>The users value.</summary>
    public List<EstateUser>? Users { get; set; }
}

/// <summary>API data model describing estate user information.</summary>

public class EstateUser
{
    /// <summary>The user id value.</summary>
    public Guid UserId { get; set; }
    /// <summary>The email address value.</summary>
    public string? EmailAddress { get; set; }
    /// <summary>The created date time value.</summary>
    public DateTime CreatedDateTime { get; set; }
}

/// <summary>API data model describing estate operator information.</summary>

public class EstateOperator
{
    /// <summary>Unique identifier of the operator.</summary>
    public Guid OperatorId { get; set; }
    /// <summary>The name value.</summary>
    public string? Name { get; set; }
    /// <summary>The require custom merchant number value.</summary>
    public bool RequireCustomMerchantNumber { get; set; }
    /// <summary>The require custom terminal number value.</summary>
    public bool RequireCustomTerminalNumber { get; set; }
    /// <summary>The created date time value.</summary>
    public DateTime CreatedDateTime { get; set; }
}

/// <summary>API data model describing estate contract information.</summary>

public class EstateContract
{
    /// <summary>Unique identifier of the operator.</summary>
    public Guid OperatorId { get; set; }
    /// <summary>Unique identifier of the contract.</summary>
    public Guid ContractId { get; set; }
    /// <summary>The name value.</summary>
    public string? Name { get; set; }
    /// <summary>The operator name value.</summary>
    public string? OperatorName { get; set; }
}


/// <summary>API data model describing estate merchant information.</summary>


public class EstateMerchant
{
    /// <summary>Unique identifier of the merchant.</summary>
    public Guid MerchantId { get; set; }
    /// <summary>The name value.</summary>
    public string? Name { get; set; }
    /// <summary>The reference value.</summary>
    public string? Reference { get; set; }
}