namespace EstateReportingAPI.Models;

public class MerchantOpeningHour
{
    public Guid MerchantId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public String OpeningTime { get; set; }
    public String ClosingTime { get; set; }
}

public class MerchantScheduleResponse {
    public MerchantScheduleResponse() {
        this.Months = new List<MerchantScheduleMonthResponse>();
    }
    public int Year { get; set; }

    public List<MerchantScheduleMonthResponse> Months { get; set; }
}

public class MerchantScheduleMonthResponse {
    public int Month { get; set; }

    public List<int> ClosedDays { get; set; }
}