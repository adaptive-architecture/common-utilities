/*
   Based on https://github.com/xunit/samples.xunit/blob/28d3683f74b104d33544efe5d1ae45ce9b0ad8c5/v3/RetryFactExample/
*/
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

namespace AdaptArch.Common.Utilities.xUnit.Extensions.Retry;

/// <summary>
/// Works just like [Fact] except that failures are retried (by default, 3 times).
/// </summary>
[XunitTestCaseDiscoverer(typeof(RetryFactDiscoverer))]
public class RetryFactAttribute(
    [CallerFilePath] string? sourceFilePath = null,
    [CallerLineNumber] int sourceLineNumber = -1) :
        FactAttribute(sourceFilePath, sourceLineNumber)
{
    /// <summary>
    /// Gets or sets the maximum number of retries for the test case.
    /// Defaults to 3 retries if not specified.
    /// </summary>
    public int MaxRetries { get; set; } = 5;
}
