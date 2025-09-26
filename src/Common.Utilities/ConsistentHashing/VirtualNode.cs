namespace AdaptArch.Common.Utilities.ConsistentHashing;

internal sealed class VirtualNode<T> : IComparable<VirtualNode<T>>
    where T : IEquatable<T>
{
    public uint Hash { get; }
    public T Server { get; }

    public VirtualNode(uint hash, T server)
    {
        Hash = hash;
        ArgumentNullException.ThrowIfNull(server);
        Server = server;
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

    public static bool operator ==(VirtualNode<T>? left, VirtualNode<T>? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Hash == right.Hash && left.Server.Equals(right.Server);
    }

    public static bool operator !=(VirtualNode<T>? left, VirtualNode<T>? right)
    {
        return !(left == right);
    }

    public static bool operator <(VirtualNode<T>? left, VirtualNode<T>? right)
    {
        if (left is null) return right is not null;
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(VirtualNode<T>? left, VirtualNode<T>? right)
    {
        if (left is null) return true;
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(VirtualNode<T>? left, VirtualNode<T>? right)
    {
        if (left is null) return false;
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(VirtualNode<T>? left, VirtualNode<T>? right)
    {
        if (left is null) return right is null;
        return left.CompareTo(right) >= 0;
    }
}
