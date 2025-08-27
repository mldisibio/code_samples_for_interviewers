using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace contoso.ado.XmlHelp
{
    /// <summary>Based on Microsoft's recommendation to cache XmlSerializers to avoid memory leaks, this cache also includes DataContractSerializers.</summary>
    /// <remarks>See http://msdn.microsoft.com/en-us/library/system.xml.serialization.xmlserializer.aspx (Section 'Dynamically Generated Assemblies')</remarks>
    internal class Serializers
    {
        static readonly Serializers _instance = new Serializers();
        static readonly object _searchOrAdd = new object();

        readonly Dictionary<SerializerKey, XmlSerializer> _xmlCache;

        static Serializers() { }

        Serializers()
        {
            _xmlCache = new Dictionary<SerializerKey, XmlSerializer>(64);
        }

        /// <summary>Gets the Serializer cache (singleton pattern).</summary>
        internal static Serializers Cache { get { return _instance; } }

        /// <summary>Returns the appropriate <see cref="XmlSerializer"/> for <typeparamref name="T"/>.</summary>
        internal XmlSerializer GetXmlSerializer<T>()
        {
            return GetXmlSerializer<T>(null, null);
        }

        /// <summary>Returns the appropriate <see cref="XmlSerializer"/> for <typeparamref name="T"/> and the given <paramref name="root"/>.</summary>
        internal XmlSerializer GetXmlSerializer<T>(string? root)
        {
            return GetXmlSerializer<T>(root, null);
        }

        /// <summary>Returns the appropriate <see cref="XmlSerializer"/> for <typeparamref name="T"/> and the given <paramref name="root"/> and namespace <paramref name="ns"/>.</summary>
        internal XmlSerializer GetXmlSerializer<T>(string? root, string? ns)
        {
            bool rootWasSupplied = !string.IsNullOrWhiteSpace(root);
            bool nsWasSupplied = !string.IsNullOrWhiteSpace(ns);

            if (nsWasSupplied && !rootWasSupplied)
                throw new InvalidOperationException("Please supply the root element name when supplying a namespace when using the Xml or DataContract Serializer extensions.");

            XmlSerializer? serializer;

            if (rootWasSupplied)
            {
                var key = SerializerKey.Create<T>(root, ns);
                lock (_searchOrAdd)
                {
                    if (!_xmlCache.TryGetValue(key, out serializer))
                    {
                        if (nsWasSupplied)
                            serializer = new XmlSerializer(typeof(T), null, null, new XmlRootAttribute(root!), ns);
                        else
                            serializer = new XmlSerializer(typeof(T), new XmlRootAttribute(root!));
                        // these overloads are not 're-used' by the XmlSerializer infrastructure, so cache them
                        if (!_xmlCache.ContainsKey(key))
                            _xmlCache.Add(key, serializer);
                    }
                }
            }
            else
            {
                // this overload will re-use any existing XmlSerializer dynamic assembly already created, so no need to cache
                serializer = new XmlSerializer(typeof(T));
            }

            return serializer;
        }
    }
}
