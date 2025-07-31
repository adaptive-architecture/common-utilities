#nullable enable
using Xunit;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.UnitTests;

public class TestCallTracker
{
    public int CallCount { get; private set; }

    public void TrackCall()
    {
        CallCount++;
    }

    public void Success()
    {
        TrackCall();
    }

    public void Failure()
    {
        TrackCall();
        Assert.Fail("This is a failure test case.");
    }
}

/// <summary>
/// A class containing helper tests.
/// </summary>
public static class HelperTests
{

    public static XunitTestAssembly GetXunitTestAssembly() => new(typeof(TestCallTracker).Assembly);
    public static XunitTestCollection GetXunitTestCollection() => new(GetXunitTestAssembly(), null, true, "HelperTests Collection");
    public static XunitTestClass GetXunitTestClass() => new(typeof(TestCallTracker), GetXunitTestCollection());
    public static XunitTestMethod GetXunitTestMethod() => GetXunitTestMethod_Helper_Success();
    public static XunitTestMethod GetXunitTestMethod_Helper_Success() => new(GetXunitTestClass(), typeof(TestCallTracker).GetMethod(nameof(TestCallTracker.Success))!, []);
    public static XunitTestMethod GetXunitTestMethod_Helper_Failure() => new(GetXunitTestClass(), typeof(TestCallTracker).GetMethod(nameof(TestCallTracker.Failure))!, []);

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
