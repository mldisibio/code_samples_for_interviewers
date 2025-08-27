using System;

namespace contoso.ado.XmlHelp
{
    /// <summary>Represents a case-sensitive Dictionary key for caching Serializers.</summary>
    internal class SerializerKey : IEquatable<SerializerKey>
    {
        readonly int _hash;

        SerializerKey(Type targetType, string? root, string? ns)
        {
            this.TargetFullName = targetType.AssemblyQualifiedName!;
            _hash = this.TargetFullName.GetHashCode();

            if (!string.IsNullOrWhiteSpace(root))
            {
                this.Root = root;
                _hash ^= this.Root.GetHashCode();
            }

            if (!string.IsNullOrWhiteSpace(ns))
            {
                this.Namespace = ns;
                _hash ^= this.Namespace.GetHashCode();
            }
        }

        /// <summary>
        /// Get an instance of <see cref="SerializerKey"/> defined by Type <typeparamref name="T"/>
        /// and an optional namespace and/or root.
        /// </summary>
        internal static SerializerKey Create<T>(string? root, string? ns)
        {
            return new SerializerKey(typeof(T), root, ns);
        }

        public string TargetFullName { get; private set; }

        public string? Root { get; private set; }

        public string? Namespace { get; private set; }

        public override int GetHashCode() { return _hash; }

        public override string ToString()
        {
            return TargetFullName + (Root == null ? string.Empty : " : " + Root) + (Namespace == null ? string.Empty : " : " + Namespace);
        }

        public override bool Equals(object? obj) { return Equals(obj as SerializerKey); }

        public bool Equals(SerializerKey? other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (int.Equals(this._hash, other._hash))
                if (string.Equals(this.TargetFullName, other.TargetFullName, StringComparison.Ordinal))
                    if (string.Equals(this.Root, other.Root, StringComparison.Ordinal))
                        if (string.Equals(this.Namespace, other.Namespace, StringComparison.Ordinal))
                            return true;
            return false;
        }
    }
}
