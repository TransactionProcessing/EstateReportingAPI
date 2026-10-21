namespace EstateReportingAPI.DataTransferObjects{
    using System;
    using System.Collections.Generic;

    /// <summary>API data model describing merchant information.</summary>

    public class Merchant{
        #region Properties
        
        /// <summary>Unique identifier of the merchant.</summary>
        
        public Guid MerchantId{ get; set; }
        /// <summary>Numeric reporting identifier used to filter or identify a merchant.</summary>
        public Int32 MerchantReportingId{ get; set; }
        /// <summary>The name value.</summary>
        public String Name{ get; set; }
        /// <summary>The reference value.</summary>
        public String Reference { get; set; }
        /// <summary>The balance value.</summary>
        public Decimal Balance { get; set; }
        /// <summary>The settlement schedule value.</summary>
        public Int32 SettlementSchedule { get; set; }
        /// <summary>The created date time value.</summary>
        public DateTime CreatedDateTime { get; set; }

        /// <summary>The address id value.</summary>

        public Guid AddressId { get; set; }
        /// <summary>The address line1 value.</summary>
        public String AddressLine1 { get; set; }
        /// <summary>The address line2 value.</summary>
        public String AddressLine2 { get; set; }
        /// <summary>The town value.</summary>
        public String Town { get; set; }
        /// <summary>The region value.</summary>
        public String Region { get; set; }
        /// <summary>The post code value.</summary>
        public String PostCode{ get; set; }
        /// <summary>The country value.</summary>
        public String Country { get; set; }
        /// <summary>The contact id value.</summary>
        public Guid ContactId { get; set; }
        /// <summary>The contact name value.</summary>
        public String ContactName { get; set; }
        /// <summary>The contact email value.</summary>
        public String ContactEmail { get; set; }
        /// <summary>The contact phone value.</summary>
        public String ContactPhone { get; set; }

        #endregion
    }

    /// <summary>Response data for opening hours.</summary>

    public class OpeningHoursResponse
    {
        /// <summary>The day of week value.</summary>
        public Int32 DayOfWeek { get; set; }

        /// <summary>The opening value.</summary>

        public string Opening { get; set; }

        /// <summary>The closing value.</summary>

        public string Closing { get; set; }
    }

    /// <summary>API data model describing merchant operator information.</summary>

    public class MerchantOperator
    {
        /// <summary>Unique identifier of the merchant.</summary>
        public Guid MerchantId { get; set; }
        /// <summary>Unique identifier of the operator.</summary>
        public Guid OperatorId { get; set; }
        /// <summary>The operator name value.</summary>
        public String OperatorName { get; set; }
        /// <summary>The merchant number value.</summary>
        public String MerchantNumber { get; set; }
        /// <summary>The terminal number value.</summary>
        public String TerminalNumber { get; set; }
        /// <summary>The is deleted value.</summary>
        public Boolean IsDeleted { get; set; }
    }

    /// <summary>API data model describing merchant contract information.</summary>

    public class MerchantContract
    {
        /// <summary>Unique identifier of the merchant.</summary>
        public Guid MerchantId { get; set; }
        /// <summary>Unique identifier of the contract.</summary>
        public Guid ContractId { get; set; }
        /// <summary>The contract name value.</summary>
        public String ContractName { get; set; }
        /// <summary>The operator name value.</summary>
        public String OperatorName { get; set; }
        /// <summary>The is deleted value.</summary>
        public Boolean IsDeleted { get; set; }
        /// <summary>The contract products value.</summary>
        public List<MerchantContractProduct> ContractProducts { get; set; }
    }

    /// <summary>API data model describing merchant contract product information.</summary>

    public class MerchantContractProduct
    {
        /// <summary>Unique identifier of the merchant.</summary>
        public Guid MerchantId { get; set; }
        /// <summary>Unique identifier of the contract.</summary>
        public Guid ContractId { get; set; }
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
    }

    /// <summary>API data model describing merchant device information.</summary>

    public class MerchantDevice
    {
        /// <summary>Unique identifier of the merchant.</summary>
        public Guid MerchantId { get; set; }
        /// <summary>The device id value.</summary>
        public Guid DeviceId { get; set; }
        /// <summary>The device identifier value.</summary>
        public String DeviceIdentifier { get; set; }
        /// <summary>The is deleted value.</summary>
        public Boolean IsDeleted { get; set; }
    }

    /// <summary>API data model describing merchant opening hour information.</summary>

    public class MerchantOpeningHour
    {
        /// <summary>Unique identifier of the merchant.</summary>
        public Guid MerchantId { get; set; }
        /// <summary>The day of week value.</summary>
        public DayOfWeek DayOfWeek { get; set; }
        /// <summary>The opening time value.</summary>
        public String OpeningTime { get; set; }
        /// <summary>The closing time value.</summary>
        public String ClosingTime { get; set; }
    }

    /// <summary>Response data for merchant schedule.</summary>

    public class MerchantScheduleResponse
    {
        /// <summary>The year value.</summary>
        public int Year { get; set; }

        /// <summary>The months value.</summary>

        public List<MerchantScheduleMonthResponse> Months { get; set; }
    }

    /// <summary>Response data for merchant schedule month.</summary>

    public class MerchantScheduleMonthResponse
    {
        /// <summary>The month value.</summary>
        public int Month { get; set; }

        /// <summary>Day numbers within the month on which the merchant is closed.</summary>

        public List<int> ClosedDays { get; set; }
    }
}