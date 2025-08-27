using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace contoso.ado.XmlHelp
{
    /// <summary>Extensions enabling inline deserialization calls over an <see cref="XmlReader"/>.</summary>
    internal static class SerializationHelp
    {
        const string _dummyNS = "http://slateblue.adoframework.xmlserialization/ns";
        readonly static XmlSerializerNamespaces _emptyNamespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", _dummyNS) });

        /// <summary>
        /// Deserializes an xml string with the root element <paramref name="root"/> and namespace <paramref name="ns"/>
        /// to an instance of <typeparamref name="T"/> using a <see cref="System.Xml.Serialization.XmlSerializer"/>.
        /// If the operation fails, returns a new instance of <typeparamref name="T"/> using the default constructor.
        /// </summary>
        public static T DeserializeXmlOrCreate<T>(this XmlReader xmlReader, string root, string? ns)
            where T : class, new()
        {
            try
            {
                var deserializer = Serializers.Cache.GetXmlSerializer<T>(root, ns);
                return (deserializer.Deserialize(xmlReader) as T) ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        /// <summary>Serialize <paramref name="src"/> to an xml string using the supplied directives.</summary>
        /// <param name="src">The object to serialize.</param>
        /// <param name="omitDeclaration">True to omit the XML declaration.</param>
        /// <param name="omitNamespaces">True to not emit any namespace declarations, including 'xsd' and 'xsi'.</param>
        /// <param name="indented">True to indent elements.</param>
        public static string? SerializeFromXmlContract<T>(this T src, bool omitDeclaration, bool omitNamespaces, bool indented)
        {
            if (src == null)
                return null;

            if (omitDeclaration || indented)
            {
                XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = omitDeclaration, Indent = indented };
                if (omitNamespaces)
                    return src.SerializeFromXmlContract(xws, _emptyNamespaces);
                else
                    return src.SerializeFromXmlContract(xws);

            }
            else if (omitNamespaces)
                return src.SerializeFromXmlContract(_emptyNamespaces);
            else
                return src.SerializeFromXmlContract();
        }

        /// <summary>Serialize <paramref name="src"/> to an xml string using its <see cref="XmlSerializer"/>.</summary>
        static string? SerializeFromXmlContract<T>(this T src)
        {
            if (src == null)
                return null;
            var serializer = Serializers.Cache.GetXmlSerializer<T>();
            using MemoryStream ms = new MemoryStream(1024);
            serializer.Serialize(ms, src);
            ms.Position = 0;
            using StreamReader reader = new StreamReader(ms);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Serialize <paramref name="src"/> to an xml string using its <see cref="XmlSerializer"/>
        /// and the supplied <see cref="XmlWriterSettings"/>
        /// </summary>
        static string? SerializeFromXmlContract<T>(this T src, XmlWriterSettings xws)
        {
            if (src == null)
                return null;
            var serializer = Serializers.Cache.GetXmlSerializer<T>();
            using MemoryStream ms = new MemoryStream(1024);
            XmlWriter xw = XmlTextWriter.Create(ms, xws);
            serializer.Serialize(xw, src);
            ms.Position = 0;
            using StreamReader reader = new StreamReader(ms);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Serialize <paramref name="src"/> to an xml string using its <see cref="XmlSerializer"/>
        /// and the supplied collection of namespaces.
        /// </summary>
        static string? SerializeFromXmlContract<T>(this T src, XmlSerializerNamespaces xsn)
        {
            if (src == null)
                return null;
            var serializer = Serializers.Cache.GetXmlSerializer<T>();
            using MemoryStream ms = new MemoryStream(1024);
            serializer.Serialize(ms, src, xsn);
            ms.Position = 0;
            using StreamReader reader = new StreamReader(ms);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Serialize <paramref name="src"/> to an xml string using its <see cref="XmlSerializer"/>
        /// and the supplied <see cref="XmlWriterSettings"/> and the supplied collection of namespaces.
        /// </summary>
        static string? SerializeFromXmlContract<T>(this T src, XmlWriterSettings xws, XmlSerializerNamespaces xns)
        {
            if (src == null)
                return null;
            var serializer = Serializers.Cache.GetXmlSerializer<T>();
            using MemoryStream ms = new MemoryStream(1024);
            XmlWriter xw = XmlTextWriter.Create(ms, xws);
            serializer.Serialize(xw, src, xns);
            ms.Position = 0;
            using StreamReader reader = new StreamReader(ms);
            return reader.ReadToEnd();
        }
    }
}
