namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <summary>
/// Configuration options for <see cref="NullMessageHub"/>.
/// This is a null object implementation that provides no additional configuration beyond
/// the base <see cref="MessageHubOptions"/> functionality.
/// </summary>
/// <remarks>
/// <para>
/// Since <see cref="NullMessageHub"/> performs no actual operations, this options class
/// serves primarily as a type-safe configuration contract. The inherited base options
/// (such as <see cref="MessageHubOptions.OnMessageHandlerError"/>) are available but
/// will never be invoked since no message handlers are actually executed.
/// </para>
/// <para>
/// This class is sealed to prevent inheritance and follows the null object pattern
/// by providing a minimal, do-nothing configuration implementation.
/// </para>
/// </remarks>
public sealed class NullMessageHubOptions : MessageHubOptions;
