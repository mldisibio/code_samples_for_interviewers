using System;
using System.Data;
using System.Data.Common;
using contoso.ado.Internals;

namespace contoso.ado.Common
{
    /// <summary>Fluent extension helpers over <see cref="DbParameter"/>.</summary>
    public static class DbParameterExtensions
    {
        /// <summary>Set the given <paramref name="dbParameter"/> as a nullable output parameter.</summary>
        public static DbParameter ForOutput(this DbParameter dbParameter)
        {
            dbParameter.ThrowIfNull().Direction = ParameterDirection.Output;
            dbParameter.IsNullable = true;
            return dbParameter;
        }

        /// <summary>Set the given <paramref name="dbParameter"/> as bot an input and output parameter.</summary>
        public static DbParameter ForInputOutput(this DbParameter dbParameter)
        {
            dbParameter.ThrowIfNull().Direction = ParameterDirection.InputOutput;
            return dbParameter;
        }

        /// <summary>Set the given <paramref name="dbParameter"/> as bot an input and output parameter.</summary>
        public static DbParameter ForReturnValue(this DbParameter dbParameter)
        {
            dbParameter.ThrowIfNull().Direction = ParameterDirection.ReturnValue;
            dbParameter.IsNullable = true;
            return dbParameter;
        }

        /// <summary>Set the given <paramref name="dbParameter"/> as nullable.</summary>
        public static DbParameter AsNullable(this DbParameter dbParameter)
        {
            dbParameter.ThrowIfNull().IsNullable = true;
            return dbParameter;
        }

        /// <summary>Set the given <paramref name="dbParameter"/> to the binary (bytes) or string (characters) size <paramref name="size"/>.</summary>
        public static DbParameter OfSize(this DbParameter dbParameter, int size)
        {
            dbParameter.ThrowIfNull().Size = size;
            return dbParameter;
        }

        /// <summary>
        /// Set the value of the the given <paramref name="dbParameter"/>.
        /// If the parameter type has not yet been explicitly set, it will be correctly inferred from <paramref name="value"/>.
        /// Explicitly setting <paramref name="value"/> to <see langword="null"/> will be interpreted here as <see cref="P:System.DbNull.Value"/>,
        /// which is an explicit parameter value and not the same as the directive to use its default value.  
        /// </summary>
        public static DbParameter WithValue(this DbParameter dbParameter, object value)
        {
            dbParameter.ThrowIfNull().Value = value ?? DBNull.Value;
            return dbParameter;
        }

        /// <summary>
        /// Sets the value of the the given <paramref name="dbParameter"/> to <see langword="null"/> (not <see cref="P:System.DbNull.Value"/>) so that
        /// it does not send any value to the database, and the default parameter value is used instead.
        /// </summary>
        public static DbParameter WithNoValue(this DbParameter dbParameter)
        {
            dbParameter.ThrowIfNull().Value = null;
            return dbParameter;
        }

        /// <summary>Add or replace the given  <paramref name="dbParameter"/> to the given <paramref name="dbCommand"/> parameter collection.</summary>
        public static DbParameter AndAddTo(this DbParameter dbParameter, DbCommand dbCommand)
        {
            if (dbParameter.IsValid())
            {
                // remove the parameter if it already exists
                if (dbCommand.ThrowIfNull().Parameters.Count > 0)
                {
                    if (dbParameter.ParameterName.IsNotNullOrEmptyString())
                        if (dbCommand.Parameters.Contains(dbParameter.ParameterName))
                            dbCommand.Parameters.RemoveAt(dbParameter.ParameterName);
                }

                // add the parameter
                dbCommand.Parameters.Add(dbParameter);
            }
            return dbParameter;
        }

        /// <summary>True if <paramref name="dbParameter"/> is not <see langword="null"/> and its ParameterName is not empty.</summary>
        internal static bool IsValid(this DbParameter dbParameter)
        {
            dbParameter.ThrowIfNull();
            if (dbParameter.ParameterName.IsNullOrEmptyString())
                throw new InvalidOperationException("Cannot add an unnamed parameter.");
            return true;
        }

        /// <summary>
        /// Returns true if the <paramref name="dbParameter"/> is not 'DBNull'. 
        /// <paramref name="value"/> will hold the value of <paramref name="dbParameter"/>, or the default for <typeparamref name="T"/>.
        /// This extension is primarily intended to retreive output values that are not expected to be null.
        /// </summary>
        public static bool TryGetValue<T>(this DbParameter dbParameter, out T? value)
        {
            value = default;
            if (dbParameter == null || dbParameter.Value == DBNull.Value)
                return false;
            else
            {
                value = (T?)dbParameter.Value;
                return true;
            }
        }

        #region DbType Extensions

        // -------------------------------------------------------------------------------
        // http://msdn.microsoft.com/EN-US/library/wf35eysz(v=VS.110,d=hv.2).aspx
        // When setting command parameters, the SqlDbType and DbType are linked.
        // Therefore, setting the DbType changes the SqlDbType to a supporting SqlDbType.
        // -------------------------------------------------------------------------------

        /// <summary>Explicitly set the <see cref="T:System.Data.DbType"/> of <paramref name="dbParameter"/>.</summary>
        public static _DbTypeWrapper AsDbType(this DbParameter dbParameter)
        {
            return new _DbTypeWrapper(dbParameter);
        }

        /// <summary>Internal wrapper to enable fluent setting of <see cref="T:System.Data.DbType"/>.</summary>
        public sealed class _DbTypeWrapper
        {
            readonly DbParameter p;
            internal _DbTypeWrapper(DbParameter p)
            {
                this.p = p.ThrowIfNull();
            }

            /// <summary>(=SqlDbType.VarChar) A variable-length stream of non-Unicode characters ranging between 1 and 8,000 characters.</summary>
            public DbParameter AnsiString { get { p.DbType = DbType.AnsiString; return p; } }
            /// <summary>(=SqlDbType.VarBinary) A variable-length stream of binary data ranging between 1 and 8,000 bytes.</summary>
            public DbParameter Binary { get { p.DbType = DbType.Binary; return p; } }
            /// <summary>(=SqlDbType.TinyInt) An 8-bit unsigned integer ranging in value from 0 to 255.</summary>
            public DbParameter Byte { get { p.DbType = DbType.Byte; return p; } }
            /// <summary>(=SqlDbType.Bit) A simple type representing Boolean values of true or false.</summary>
            public DbParameter Boolean { get { p.DbType = DbType.Boolean; return p; } }
            /// <summary>(=SqlDbType.Money) A currency value ranging from -2 63 (or -922,337,203,685,477.5808) to 2 63 -1 (or +922,337,203,685,477.5807) with an accuracy to a ten-thousandth of a currency unit.</summary>
            public DbParameter Currency { get { p.DbType = DbType.Currency; return p; } }
            /// <summary>(=SqlDbType.DateTime) A type representing a date value.</summary>
            public DbParameter Date { get { p.DbType = DbType.Date; return p; } }
            /// <summary>(=SqlDbType.DateTime) A type representing a date and time value.</summary>
            public DbParameter DateTime { get { p.DbType = DbType.DateTime; return p; } }
            /// <summary>(=SqlDbType.Decimal) A simple type representing values ranging from 1.0 x 10 -28 to approximately 7.9 x 10 28 with 28-29 significant digits.</summary>
            public DbParameter Decimal { get { p.DbType = DbType.Decimal; return p; } }
            /// <summary>(=SqlDbType.Float) A floating point type representing values ranging from approximately 5.0 x 10 -324 to 1.7 x 10 308 with a precision of 15-16 digits.</summary>
            public DbParameter Double { get { p.DbType = DbType.Double; return p; } }
            /// <summary>(=SqlDbType.UniqueIdentifier) A globally unique identifier (or GUID).</summary>
            public DbParameter Guid { get { p.DbType = DbType.Guid; return p; } }
            /// <summary>(=SqlDbType.SmallInt) An integral type representing signed 16-bit integers with values between -32,768 and 32,767.</summary>
            public DbParameter Int16 { get { p.DbType = DbType.Int16; return p; } }
            /// <summary>(=SqlDbType.Int) An integral type representing signed 32-bit integers with values between -2,147,483,648 and 2,147,483,647.</summary>
            public DbParameter Int32 { get { p.DbType = DbType.Int32; return p; } }
            /// <summary>(=SqlDbType.BigInt) An integral type representing signed 64-bit integers with values between -9,223,372,036,854,775,808 and 9,223,372,036,854,775,807.</summary>
            public DbParameter Int64 { get { p.DbType = DbType.Int64; return p; } }
            /// <summary>(=SqlDbType.Variant) A general type representing any reference or value type not explicitly represented by another DbType value.</summary>
            public DbParameter Object { get { p.DbType = DbType.Object; return p; } }
            /// <summary>(Unsupported SqlDbType) An integral type representing signed 8-bit integers with values between -128 and 127.</summary>
            public DbParameter SByte { get { p.DbType = DbType.SByte; return p; } }
            /// <summary>(=SqlDbType.Real) A floating point type representing values ranging from approximately 1.5 x 10 -45 to 3.4 x 10 38 with a precision of 7 digits.</summary>
            public DbParameter Single { get { p.DbType = DbType.Single; return p; } }
            /// <summary>(=SqlDbType.NVarChar) A type representing Unicode character strings.</summary>
            public DbParameter String { get { p.DbType = DbType.String; return p; } }
            /// <summary>(=SqlDbType.DateTime) A type representing a SQL Server DateTime value. If you want to use a SQL Server time value, use <see cref="F:System.Data.SqlDbType.Time" />.</summary>
            public DbParameter Time { get { p.DbType = DbType.Time; return p; } }
            /// <summary>(Unsupported SqlDbType) An integral type representing unsigned 16-bit integers with values between 0 and 65535.</summary>
            public DbParameter UInt16 { get { p.DbType = DbType.UInt16; return p; } }
            /// <summary>(Unsupported SqlDbType) An integral type representing unsigned 32-bit integers with values between 0 and 4294967295.</summary>
            public DbParameter UInt32 { get { p.DbType = DbType.UInt32; return p; } }
            /// <summary>(Unsupported SqlDbType) An integral type representing unsigned 64-bit integers with values between 0 and 18446744073709551615.</summary>
            public DbParameter UInt64 { get { p.DbType = DbType.UInt64; return p; } }
            /// <summary>(Unsupported SqlDbType) A variable-length numeric value.</summary>
            public DbParameter VarNumeric { get { p.DbType = DbType.VarNumeric; return p; } }
            /// <summary>(=SqlDbType.Char) A fixed-length stream of non-Unicode characters.</summary>
            public DbParameter AnsiStringFixedLength { get { p.DbType = DbType.AnsiStringFixedLength; return p; } }
            /// <summary>(=SqlDbType.NChar) A fixed-length string of Unicode characters.</summary>
            public DbParameter StringFixedLength { get { p.DbType = DbType.StringFixedLength; return p; } }
            /// <summary>(=SqlDbType.Xml) A parsed representation of an XML document or fragment.</summary>
            public DbParameter Xml { get { p.DbType = DbType.Xml; return p; } }
            /// <summary>(=SqlDbType.DateTime2) Date and time data. Date value range is from January 1,1 AD through December 31, 9999 AD. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds.</summary>
            public DbParameter DateTime2 { get { p.DbType = DbType.DateTime2; return p; } }
            /// <summary>(=SqlDbType.DateTimeOffset) Date and time data with time zone awareness. Date value range is from January 1,1 AD through December 31, 9999 AD. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds. Time zone value range is -14:00 through +14:00. </summary>
            public DbParameter DateTimeOffset { get { p.DbType = DbType.DateTimeOffset; return p; } }
        }

        #endregion

    }
}
