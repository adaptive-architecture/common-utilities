using AdaptArch.Common.Utilities.Extensions;

namespace AdaptArch.Common.Utilities.UnitTests.Extensions;

public class ExceptionExtensionsSpecs
{
    [Fact]
    public void NotSupported_Behavior()
    {
        var nullObject = (object)null;
        var notNullObject = new object();
        ExceptionExtensions.ThrowNotSupportedIfNotNull(nullObject, "null");
        ExceptionExtensions.ThrowNotSupportedIfNull(notNullObject, "null");

        Assert.Throws<NotSupportedException>(() => ExceptionExtensions.ThrowNotSupportedIfNull<object>(null, "null"));
        Assert.Throws<NotSupportedException>(() => ExceptionExtensions.ThrowNotSupportedIfNotNull(new object(), "null"));
    }
}
