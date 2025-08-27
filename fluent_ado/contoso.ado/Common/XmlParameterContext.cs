using System;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Xml;
using contoso.ado.XmlHelp;
using contoso.ado.Internals;

namespace contoso.ado.Common
{
    /// <summary>
    /// A factory for fluently serializing instance of {T} into a Sql Server Xml parameter
    /// </summary>
    public sealed class XmlParameterContext
    {
        /// <summary>The <see cref="Database"/> context for this instance.</summary>
        readonly Database _dbContext;

        internal XmlParameterContext(Database db)
        {
            _dbContext = db.ThrowIfNull();
        }

        /// <summary>The source type which will be serialized into an xml parameter.</summary>
        public XmlParamBuilder<T> FromSource<T>(T data) where T : class, new()
        {
            return new XmlParamBuilder<T>(data, _dbContext);
        }

        /// <summary>A wrapper class for invoking creation of the parameter with fluent syntax.</summary>
        public class XmlParamBuilder<T> where T : class, new()
        {
            readonly T _data;
            readonly Database _ctx;

            internal XmlParamBuilder(T data, Database ctx)
            {
                _data = data;
                _ctx = ctx;
            }

            /// <summary>Returns a <see cref="DbParameter"/> structured for piping the given xml content.</summary>
            public DbParameter ToXmlParameter(string paramName)
            {
                ParamCheck.Assert.IsNotNull(_ctx, "SqlDbContext");
                ParamCheck.Assert.IsNotNull(_data, "data");
                ParamCheck.Assert.IsNotNullOrEmpty(paramName, "paramName");

                // serialize the given data object into an xml database parameter
                DbParameter xmlData;
                using (var strReader = new StringReader(_data.SerializeFromXmlContract(true, true, true)!))
                {
                    using var xmlReader = new XmlTextReader(strReader);
                    xmlData = _ctx.CreateXmlParameter(paramName).WithValue(new SqlXml(xmlReader));
                }
                return xmlData;
            }
        }
    }
}
