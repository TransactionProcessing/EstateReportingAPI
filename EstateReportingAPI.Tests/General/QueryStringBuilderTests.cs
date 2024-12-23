using System;
using Shouldly;
using Xunit;

namespace EstateReportingAPI.Tests.General;

public class QueryStringBuilderTests
{
    [Theory]
    [InlineData("StringTest", "value",true)]
    [InlineData("IntegerTest", 10, true)]
    [InlineData("DecimalTest1", 1.00, true)]
    [InlineData("DecimalTest2", 0.01, true)]
    [InlineData("GuidTest", "69F35754-EB7D-441B-A62F-F50633AFBE91", true)]
    [InlineData("NullTest", null, false)]
    [InlineData("EmptyStringTest", "", false)]
    [InlineData("ZeroIntegerTest", 0, false)]
    [InlineData("ZeroDecimalTest1", 0, false)]
    [InlineData("ZeroDecimalTest2", 0.00, false)]
    [InlineData("EmptyGuidTest", "00000000-0000-0000-0000-000000000000", false)]
    public void QueryStringBuilderTests_BuildQueryString_ValuesAddedAsExpected(String keyname, Object value, Boolean isAdded){
        if (keyname == "GuidTest" || keyname == "EmptyGuidTest")
            value = Guid.Parse(value.ToString());

        QueryStringBuilder builder = new QueryStringBuilder();
        builder.AddParameter(keyname, value);

        String queryString = builder.BuildQueryString();

        queryString.Contains(keyname).ShouldBe(isAdded);
            
    }
}