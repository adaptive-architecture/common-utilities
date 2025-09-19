namespace AdaptArch.Common.Utilities.ConsistentHashing;

internal sealed class VirtualNode<T> : IComparable<VirtualNode<T>>
    where T : IEquatable<T>
{
    public uint Hash { get; }
    public T Server { get; }

    public VirtualNode(uint hash, T server)
    {
        Hash = hash;
        Server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public int CompareTo(VirtualNode<T>? other)
    {
        if (other is null) return 1;
        return Hash.CompareTo(other.Hash);
    }

    public override bool Equals(object? obj)
    {
        return obj is VirtualNode<T> other && Hash == other.Hash && Server.Equals(other.Server);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Hash, Server);
    }
}
