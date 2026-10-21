using System;

namespace EstateReportingAPI.DataTransferObjects;

/// <summary>API data model describing value range information.</summary>

public class ValueRange{
    /// <summary>The start value value.</summary>
    public Decimal StartValue{ get; set; }
    /// <summary>The end value value.</summary>
    public Decimal EndValue{ get; set; }
}