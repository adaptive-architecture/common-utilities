namespace AdaptArch.Common.Utilities.Delay.Contracts;

/// <summary>
/// The type of delay.
/// </summary>
public enum DelayType
{
    /// <summary>
    /// Unknown type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The delays will have the same value.
    /// </summary>
    Constant = 1,

    /// <summary>
    /// The delays will increase/decrease linearly.
    /// </summary>
    Linear = 2,

    /// <summary>
    /// The delays will increase/decrease following a power of 2 pattern.
    /// </summary>
    PowerOf2 = 3,

    /// <summary>
    /// The delays will increase/decrease following a power of e(2.71..) pattern.
    /// </summary>
    Exponential = 4
}
