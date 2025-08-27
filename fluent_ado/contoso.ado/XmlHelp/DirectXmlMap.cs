using System;
using System.Xml;

namespace contoso.ado.XmlHelp
{
    /// <summary>
    /// A factory for fluently creating a delegate for mapping an <see cref="XmlReader"/>
    /// by directly deserializes the xml to an instance of {T}.
    /// </summary>
    public sealed class DirectXmlMap
    {
        DirectXmlMap() { }

        /// <summary>The target type to which the xml will be deserialized.</summary>
        public static _XmlMap<T> ForType<T>() where T : class, new()
        {
            return new _XmlMap<T>();
        }

        /// <summary>This is just a mechanism to invoke creation of the delegate with a fluent syntax.</summary>
        public sealed class _XmlMap<T> where T : class, new()
        {
            internal _XmlMap() { }

            /// <summary>
            /// Returns a <see cref="Func{XmlReader, T}"/> instance to be used by the ado infrastructure
            /// to deserialize the xml to an instance of <typeparamref name="T"/>.
            /// </summary>
            /// <param name="root">The root element name of the incoming xml.</param>
            /// <param name="ns">The custom namespace used in the incoming xml, if any.</param>
            public Func<XmlReader, T> FromDocWithRoot(string root, string? ns = null)
            {
                if (string.IsNullOrWhiteSpace(root))
                    throw new ArgumentNullException(nameof(root), "XmlParsingDelegate requires the root element name of the input xml.");

                return reader => reader.DeserializeXmlOrCreate<T>(root, ns);
            }            
        }
    }
}
