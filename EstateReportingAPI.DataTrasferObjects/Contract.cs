using System;
using System.Collections.Generic;

namespace EstateReportingAPI.DataTransferObjects;

public class Contract
{
    #region Properties
    public Guid EstateId { get; set; }
    public Int32 EstateReportingId { get; set; }
    public Guid ContractId { get; set; }
    public Int32 ContractReportingId { get; set; }
    public String Description { get; set; }
    public String OperatorName { get; set; }
    public Guid OperatorId { get; set; }
    public Int32 OperatorReportingId { get; set; }
    public List<ContractProduct> Products { get; set; }
    #endregion
}

public class ContractProduct
{
    public Guid ContractId { get; set; }
    public Int32 ContractProductReportingId { get; set; }

    public Guid ProductId { get; set; }
    public String ProductName { get; set; }
    public String DisplayText { get; set; }
    public Int32 ProductType { get; set; }
    public Decimal? Value { get; set; }
    public List<ContractProductTransactionFee> TransactionFees { get; set; }
}

public class ContractProductTransactionFee {
    public Guid TransactionFeeId { get; set; }
    public Int32 ContractProductTransactionFeeReportingId { get; set; }

    public string? Description { get; set; }
    public Int32 CalculationType { get; set; }
    public Int32 FeeType { get; set; }
    public Decimal Value { get; set; }
}


public class Estate
{
    public Guid EstateId { get; set; }
    public string? EstateName { get; set; }
    public string? Reference { get; set; }
    public List<EstateOperator>? Operators { get; set; }
    public List<EstateMerchant>? Merchants { get; set; }
    public List<EstateContract>? Contracts { get; set; }
    public List<EstateUser>? Users { get; set; }
}

public class EstateUser
{
    public Guid UserId { get; set; }
    public string? EmailAddress { get; set; }
    public DateTime CreatedDateTime { get; set; }
}

public class EstateOperator
{
    public Guid OperatorId { get; set; }
    public string? Name { get; set; }
    public bool RequireCustomMerchantNumber { get; set; }
    public bool RequireCustomTerminalNumber { get; set; }
    public DateTime CreatedDateTime { get; set; }
}

public class EstateContract
{
    public Guid OperatorId { get; set; }
    public Guid ContractId { get; set; }
    public string? Name { get; set; }
    public string? OperatorName { get; set; }
}


public class EstateMerchant
{
    public Guid MerchantId { get; set; }
    public string? Name { get; set; }
    public string? Reference { get; set; }
}