namespace EstateReportingAPI.DataTransferObjects{
    using System;
    using System.Collections.Generic;

    public class Merchant{
        #region Properties
        
        public Guid MerchantId{ get; set; }
        public Int32 MerchantReportingId{ get; set; }
        public String Name{ get; set; }
        public String Reference { get; set; }
        public Decimal Balance { get; set; }
        public Int32 SettlementSchedule { get; set; }
        public DateTime CreatedDateTime { get; set; }

        public Guid AddressId { get; set; }
        public String AddressLine1 { get; set; }
        public String AddressLine2 { get; set; }
        public String Town { get; set; }
        public String Region { get; set; }
        public String PostCode{ get; set; }
        public String Country { get; set; }
        public Guid ContactId { get; set; }
        public String ContactName { get; set; }
        public String ContactEmail { get; set; }
        public String ContactPhone { get; set; }

        #endregion
    }

    public class OpeningHoursResponse
    {
        public Int32 DayOfWeek { get; set; }

        public string Opening { get; set; }

        public string Closing { get; set; }
    }

    public class MerchantOperator
    {
        public Guid MerchantId { get; set; }
        public Guid OperatorId { get; set; }
        public String OperatorName { get; set; }
        public String MerchantNumber { get; set; }
        public String TerminalNumber { get; set; }
        public Boolean IsDeleted { get; set; }
    }

    public class MerchantContract
    {
        public Guid MerchantId { get; set; }
        public Guid ContractId { get; set; }
        public String ContractName { get; set; }
        public String OperatorName { get; set; }
        public Boolean IsDeleted { get; set; }
        public List<MerchantContractProduct> ContractProducts { get; set; }
    }

    public class MerchantContractProduct
    {
        public Guid MerchantId { get; set; }
        public Guid ContractId { get; set; }
        public Guid ProductId { get; set; }
        public String ProductName { get; set; }
        public String DisplayText { get; set; }
        public Int32 ProductType { get; set; }
        public Decimal? Value { get; set; }
    }

    public class MerchantDevice
    {
        public Guid MerchantId { get; set; }
        public Guid DeviceId { get; set; }
        public String DeviceIdentifier { get; set; }
        public Boolean IsDeleted { get; set; }
    }

    public class MerchantOpeningHour
    {
        public Guid MerchantId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public String OpeningTime { get; set; }
        public String ClosingTime { get; set; }
    }

    public class MerchantScheduleResponse
    {
        public int Year { get; set; }

        public List<MerchantScheduleMonthResponse> Months { get; set; }
    }

    public class MerchantScheduleMonthResponse
    {
        public int Month { get; set; }

        public List<int> ClosedDays { get; set; }
    }
}