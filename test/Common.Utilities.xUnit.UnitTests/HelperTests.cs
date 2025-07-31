#nullable enable
using Xunit;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.UnitTests;

/// <summary>
/// A class containing helper tests.
/// </summary>
public class HelperTests
{
    public static bool SkipWhen => true;

    [Fact]
    public void Helper_Success() => Assert.True(true);

    [Fact(SkipWhen = nameof(SkipWhen))]
    public void Helper_Failure() => Assert.Fail("This is a failure test case.");


    public static XunitTestAssembly GetXunitTestAssembly() => new(typeof(HelperTests).Assembly);
    public static XunitTestCollection GetXunitTestCollection() => new(GetXunitTestAssembly(), null, true, "HelperTests Collection");
    public static XunitTestClass GetXunitTestClass() => new(typeof(HelperTests), GetXunitTestCollection());
    public static XunitTestMethod GetXunitTestMethod() => GetXunitTestMethod_Helper_Success();
    public static XunitTestMethod GetXunitTestMethod_Helper_Success() => new(GetXunitTestClass(), typeof(HelperTests).GetMethod(nameof(Helper_Success))!, []);
    public static XunitTestMethod GetXunitTestMethod_Helper_Failure() => new(GetXunitTestClass(), typeof(HelperTests).GetMethod(nameof(Helper_Failure))!, []);

    public static XunitTestCase GetXunitTestCase(XunitTestMethod? xunitTestMethod = null) => new(
        xunitTestMethod ?? GetXunitTestMethod(),
        "HelperTests TestCase",
        Guid.NewGuid().ToString("N"),
        false,
        null,
        null,
        null,
        null,
        null,
        null,
        [],
        null,
        null,
        null
    );
}
