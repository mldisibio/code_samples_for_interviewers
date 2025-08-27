using System;
using System.Data.Common;
using contoso.ado.Internals;
using Npgsql;
using NpgsqlTypes;

namespace contoso.ado.PostgreSql
{
    /// <summary>Fluent extension helpers over <see cref="NpgsqlParameter"/>.</summary>
    public static class NpgsqlParameterExtensions
    {
        /// <summary>Cast <paramref name="dbParameter"/> to <see cref="NpgsqlParameter"/>. Will throw if cast is invalid.</summary>
        public static NpgsqlParameter AsNpgsqlParameter(this DbParameter dbParameter)
        {
            dbParameter.ThrowIfNull();

            if (dbParameter is NpgsqlParameter npgsqlParam)
                return npgsqlParam;
            else
                throw new InvalidCastException("The given DbParameter is not a NpgsqlParameter.");
        }

        /// <summary>Set the number of decimal places to which a decimal value of the given <paramref name="npgsqlParameter"/> is resolved.</summary>
        public static NpgsqlParameter OfScale(this NpgsqlParameter npgsqlParameter, byte scale)
        {
            npgsqlParameter.ThrowIfNull().Scale = scale;
            return npgsqlParameter;
        }

        /// <summary>Set the maximum number of digits used to represent the decimal value of the given <paramref name="npgsqlParameter"/>.</summary>
        public static NpgsqlParameter OfPrecision(this NpgsqlParameter npgsqlParameter, byte precision)
        {
            npgsqlParameter.ThrowIfNull().Precision = precision;
            return npgsqlParameter;
        }

        /// <summary>
        /// Set the value of the the given <paramref name="npgsqlParameter"/> using SQL types.
        /// If the parameter type has not yet been explicitly set, it will be correctly inferred from <paramref name="value"/>.
        /// Explicitly setting <paramref name="value"/> to <see langword="null"/> will be interpreted here as <see cref="P:System.DbNull.Value"/>,
        /// which is an explicit parameter value and not the same as the directive to use its default value.  
        /// </summary>
        public static NpgsqlParameter WithNpgsqlValue(this NpgsqlParameter npgsqlParameter, object value)
        {
            npgsqlParameter.ThrowIfNull().NpgsqlValue = value ?? DBNull.Value;
            return npgsqlParameter;
        }

        /// <summary>
        /// Assign the name of a 'user-defined-type' (created in Postgres using 'CREATE TYPE' when mapping parameters
        /// to enums or composite types that have been made known to the api (via NpgsqlConnection.GlobalTypeMapper.MapEnum or NpgsqlConnection.GlobalTypeMapper.MapComposite).
        /// </summary>
        public static NpgsqlParameter WithUserDefinedTypeName(this NpgsqlParameter npgsqlParameter, string userDefinedTypeName)
        {
            ParamCheck.ThrowIfNullOrEmpty(userDefinedTypeName, "userDefinedTypeName");
            npgsqlParameter.ThrowIfNull().DataTypeName = userDefinedTypeName;
            return npgsqlParameter;
        }

        #region NpgsqlDbType Extensions

        /// <summary>Explicitly set the <see cref="NpgsqlParameter.NpgsqlDbType"/> of <paramref name="dbParameter"/>.</summary>
        public static _NpgsqlDbTypeWrapper AsNpgsqlDbType(this DbParameter dbParameter)
        {
            return new _NpgsqlDbTypeWrapper(dbParameter.AsNpgsqlParameter());
        }

        /// <summary>Internal wrapper to enable fluent setting of <see cref="NpgsqlDbType"/>.</summary>
        public sealed class _NpgsqlDbTypeWrapper
        {
            readonly NpgsqlParameter p;
            internal _NpgsqlDbTypeWrapper(NpgsqlParameter p)
            {
                this.p = p.ThrowIfNull();
            }

            /// <summary><see cref="T:System.Boolean" />. true/false; yes/no; on/off; 1/0;</summary>
            public NpgsqlParameter Boolean { get { p.NpgsqlDbType = NpgsqlDbType.Boolean; return p; } }
            /// <summary><see cref="T:System.Int64" />. A 64-bit signed integer. (-9,223,372,036,854,775,808 to 9,223,372,036,854,775,807)</summary>
            public NpgsqlParameter Bigint { get { p.NpgsqlDbType = NpgsqlDbType.Bigint; return p; } }
            /// <summary><see cref="T:System.Double" />. A floating point number (inexact, variable precision) with a range of around 1E-307 to 1E+308 with a precision of at least 15 digits.</summary>
            public NpgsqlParameter Double { get { p.NpgsqlDbType = NpgsqlDbType.Double; return p; } }
            /// <summary><see cref="T:System.Int32" />. A 32-bit signed integer. (-2,147,483,648 to 2,147,483,647)</summary>
            public NpgsqlParameter Integer { get { p.NpgsqlDbType = NpgsqlDbType.Integer; return p; } }
            /// <summary>Stores a currency amount with a fixed fractional precision.</summary>
            public NpgsqlParameter Money { get { p.NpgsqlDbType = NpgsqlDbType.Money; return p; } }
            /// <summary>A floating point number (exact, user-specified precision) with up to 131072 digits before the decimal point; up to 16383 digits after the decimal point.</summary>
            public NpgsqlParameter Numeric { get { p.NpgsqlDbType = NpgsqlDbType.Numeric; return p; } }
            /// <summary><see cref="T:System.Single" />. A floating point number (inexact, variable precision) with a range of around 1E-37 to 1E+37 with a precision of at least 6 decimal digits.</summary>
            public NpgsqlParameter Real { get { p.NpgsqlDbType = NpgsqlDbType.Real; return p; } }
            /// <summary><see cref="T:System.Int16" />. A 16-bit signed integer. (-32,768 to 32,767)</summary>
            public NpgsqlParameter Smallint { get { p.NpgsqlDbType = NpgsqlDbType.Smallint; return p; } }
            /// <summary><see cref="T:System.Guid" />Postgres guid.</summary>
            public NpgsqlParameter Uuid { get { p.NpgsqlDbType = NpgsqlDbType.Uuid; return p; } }

            /// <summary><see cref="T:System.String" />. A specified-length stream of (UTF-8) characters padded with spaces.</summary>
            public NpgsqlParameter Char { get { p.NpgsqlDbType = NpgsqlDbType.Char; return p; } }
            /// <summary><see cref="T:System.String" />. Case-insensitive column type.</summary>
            public NpgsqlParameter Citext { get { p.NpgsqlDbType = NpgsqlDbType.Citext; return p; } }
            /// <summary><see cref="T:System.String" />. A specified-length stream of (UTF-8) characters. The string is limited to about 1GB.</summary>
            public NpgsqlParameter Varchar { get { p.NpgsqlDbType = NpgsqlDbType.Varchar; return p; } }
            /// <summary><see cref="T:System.String" />. A variable-length stream of (UTF-8) characters of unlimited length.</summary>
            public NpgsqlParameter Text { get { p.NpgsqlDbType = NpgsqlDbType.Text; return p; } }

            /// <summary>Postgresql date type.</summary>
            public NpgsqlParameter Date { get { p.NpgsqlDbType = NpgsqlDbType.Date; return p; } }
            /// <summary>Postgresql interval type.</summary>
            public NpgsqlParameter Interval { get { p.NpgsqlDbType = NpgsqlDbType.Interval; return p; } }
            /// <summary>Postgresql time without time zone type.</summary>
            public NpgsqlParameter Time { get { p.NpgsqlDbType = NpgsqlDbType.Time; return p; } }
            /// <summary>Postgresql timestamp without time zone type.</summary>
            public NpgsqlParameter Timestamp { get { p.NpgsqlDbType = NpgsqlDbType.Timestamp; return p; } }
            /// <summary>Postgresql timestamp with time zone type.</summary>
            public NpgsqlParameter TimestampTz { get { p.NpgsqlDbType = NpgsqlDbType.TimestampTz; return p; } }
            /// <summary>Postgresql time with time zone type.</summary>
            public NpgsqlParameter TimeTz { get { p.NpgsqlDbType = NpgsqlDbType.TimeTz; return p; } }

            /// <summary><see cref="T:System.Array" /> of type <see cref="T:System.Byte" />. A fixed-length stream of binary data ranging between 1 and 8,000 bytes.</summary>
            public NpgsqlParameter Bytea { get { p.NpgsqlDbType = NpgsqlDbType.Bytea; return p; } }
            /// <summary>A string of 1's and 0's</summary>
            public NpgsqlParameter Bit { get { p.NpgsqlDbType = NpgsqlDbType.Bit; return p; } }
            /// <summary><see cref="T:System.Array" /> of type <see cref="T:System.Byte" />. A specified-length stream of binary data.</summary>
            public NpgsqlParameter Varbit { get { p.NpgsqlDbType = NpgsqlDbType.Varbit; return p; } }

            /// <summary>Support for the json data type.</summary>
            public NpgsqlParameter Json { get { p.NpgsqlDbType = NpgsqlDbType.Json; return p; } }
            /// <summary>Support for the jsonb data type.</summary>
            public NpgsqlParameter Jsonb { get { p.NpgsqlDbType = NpgsqlDbType.Jsonb; return p; } }
            /// <summary>Support for the xml data type.</summary>
            public NpgsqlParameter Xml { get { p.NpgsqlDbType = NpgsqlDbType.Xml; return p; } }

            /// <summary>Support for tsquery.</summary>
            public NpgsqlParameter TsQuery { get { p.NpgsqlDbType = NpgsqlDbType.TsQuery; return p; } }
            /// <summary>Support for tsvector.</summary>
            public NpgsqlParameter TsVector { get { p.NpgsqlDbType = NpgsqlDbType.TsVector; return p; } }
            /// <summary>Postgresql refcursor type.</summary>
            public NpgsqlParameter Refcursor { get { p.NpgsqlDbType = NpgsqlDbType.Refcursor; return p; } }
            /// <summary>Support for allowing postgres to determine the data type.</summary>
            public NpgsqlParameter Unknown { get { p.NpgsqlDbType = NpgsqlDbType.Unknown; return p; } }
        }

        #endregion

        #region NpgsqlDbType Array Extensions

        /// <summary>Explicitly set the <see cref="NpgsqlParameter.NpgsqlDbType"/> of <paramref name="dbParameter"/> as a supported array type.</summary>
        public static _NpgsqlArrayTypeWrapper AsNpgsqlArrayOf(this DbParameter dbParameter)
        {
            return new _NpgsqlArrayTypeWrapper(dbParameter.AsNpgsqlParameter());
        }

        /// <summary>Internal wrapper to enable fluent setting of an <see cref="NpgsqlParameter"/> to a supported array type.</summary>
        public sealed class _NpgsqlArrayTypeWrapper
        {
            readonly NpgsqlParameter p;
            internal _NpgsqlArrayTypeWrapper(NpgsqlParameter p)
            {
                this.p = p.ThrowIfNull();
            }

            /// <summary><see cref="T:System.Boolean" />. true/false; yes/no; on/off; 1/0;</summary>
            public NpgsqlParameter Boolean { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Boolean; return p; } }
            /// <summary><see cref="T:System.Int64" />. A 64-bit signed integer. (-9,223,372,036,854,775,808 to 9,223,372,036,854,775,807)</summary>
            public NpgsqlParameter Bigint { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bigint; return p; } }
            /// <summary><see cref="T:System.Double" />. A floating point number (inexact, variable precision) with a range of around 1E-307 to 1E+308 with a precision of at least 15 digits.</summary>
            public NpgsqlParameter Double { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Double; return p; } }
            /// <summary><see cref="T:System.Int32" />. A 32-bit signed integer. (-2,147,483,648 to 2,147,483,647)</summary>
            public NpgsqlParameter Integer { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Integer; return p; } }
            /// <summary>Stores a currency amount with a fixed fractional precision.</summary>
            public NpgsqlParameter Money { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Money; return p; } }
            /// <summary>A floating point number (exact, user-specified precision) with up to 131072 digits before the decimal point; up to 16383 digits after the decimal point.</summary>
            public NpgsqlParameter Numeric { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Numeric; return p; } }
            /// <summary><see cref="T:System.Single" />. A floating point number (inexact, variable precision) with a range of around 1E-37 to 1E+37 with a precision of at least 6 decimal digits.</summary>
            public NpgsqlParameter Real { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Real; return p; } }
            /// <summary><see cref="T:System.Int16" />. A 16-bit signed integer. (-32,768 to 32,767)</summary>
            public NpgsqlParameter Smallint { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Smallint; return p; } }
            /// <summary><see cref="T:System.Guid" />Postgres guid.</summary>
            public NpgsqlParameter Uuid { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Uuid; return p; } }

            /// <summary><see cref="T:System.String" />. A specified-length stream of (UTF-8) characters padded with spaces.</summary>
            public NpgsqlParameter Char { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Char; return p; } }
            /// <summary><see cref="T:System.String" />. Case-insensitive column type.</summary>
            public NpgsqlParameter Citext { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Citext; return p; } }
            /// <summary><see cref="T:System.String" />. A specified-length stream of (UTF-8) characters. The string is limited to about 1GB.</summary>
            public NpgsqlParameter Varchar { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Varchar; return p; } }
            /// <summary><see cref="T:System.String" />. A variable-length stream of (UTF-8) characters of unlimited length.</summary>
            public NpgsqlParameter Text { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text; return p; } }

            /// <summary>Postgresql date type.</summary>
            public NpgsqlParameter Date { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Date; return p; } }
            /// <summary>Postgresql interval type.</summary>
            public NpgsqlParameter Interval { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Interval; return p; } }
            /// <summary>Postgresql time without time zone type.</summary>
            public NpgsqlParameter Time { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Time; return p; } }
            /// <summary>Postgresql timestamp without time zone type.</summary>
            public NpgsqlParameter Timestamp { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Timestamp; return p; } }
            /// <summary>Postgresql timestamp with time zone type.</summary>
            public NpgsqlParameter TimestampTz { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.TimestampTz; return p; } }
            /// <summary>Postgresql time with time zone type.</summary>
            public NpgsqlParameter TimeTz { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.TimeTz; return p; } }
            /// <summary>A string of 1's and 0's</summary>
            public NpgsqlParameter Bit { get { p.NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bit; return p; } }
        }

        #endregion

        #region NpgsqlDbType Range Extensions

        /// <summary>Explicitly set the <see cref="NpgsqlParameter.NpgsqlDbType"/> of <paramref name="dbParameter"/> as a supported range type.</summary>
        public static _NpgsqlRangeTypeWrapper AsNpgsqlRangeOf(this DbParameter dbParameter)
        {
            return new _NpgsqlRangeTypeWrapper(dbParameter.AsNpgsqlParameter());
        }

        /// <summary>Internal wrapper to enable fluent setting of an <see cref="NpgsqlParameter"/> to a supported array type.</summary>
        public sealed class _NpgsqlRangeTypeWrapper
        {
            readonly NpgsqlParameter p;
            internal _NpgsqlRangeTypeWrapper(NpgsqlParameter p)
            {
                this.p = p.ThrowIfNull();
            }

            /// <summary><see cref="T:System.Boolean" />. true/false; yes/no; on/off; 1/0;</summary>
            public NpgsqlParameter Boolean { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Boolean; return p; } }
            /// <summary><see cref="T:System.Int64" />. A 64-bit signed integer. (-9,223,372,036,854,775,808 to 9,223,372,036,854,775,807)</summary>
            public NpgsqlParameter Bigint { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Bigint; return p; } }
            /// <summary><see cref="T:System.Double" />. A floating point number (inexact, variable precision) with a range of around 1E-307 to 1E+308 with a precision of at least 15 digits.</summary>
            public NpgsqlParameter Double { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Double; return p; } }
            /// <summary><see cref="T:System.Int32" />. A 32-bit signed integer. (-2,147,483,648 to 2,147,483,647)</summary>
            public NpgsqlParameter Integer { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Integer; return p; } }
            /// <summary>Stores a currency amount with a fixed fractional precision.</summary>
            public NpgsqlParameter Money { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Money; return p; } }
            /// <summary>A floating point number (exact, user-specified precision) with up to 131072 digits before the decimal point; up to 16383 digits after the decimal point.</summary>
            public NpgsqlParameter Numeric { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Numeric; return p; } }
            /// <summary><see cref="T:System.Single" />. A floating point number (inexact, variable precision) with a range of around 1E-37 to 1E+37 with a precision of at least 6 decimal digits.</summary>
            public NpgsqlParameter Real { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Real; return p; } }
            /// <summary><see cref="T:System.Int16" />. A 16-bit signed integer. (-32,768 to 32,767)</summary>
            public NpgsqlParameter Smallint { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Smallint; return p; } }
            /// <summary><see cref="T:System.Guid" />Postgres guid.</summary>
            public NpgsqlParameter Uuid { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Uuid; return p; } }

            /// <summary><see cref="T:System.String" />. A specified-length stream of (UTF-8) characters padded with spaces.</summary>
            public NpgsqlParameter Char { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Char; return p; } }
            /// <summary><see cref="T:System.String" />. Case-insensitive column type.</summary>
            public NpgsqlParameter Citext { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Citext; return p; } }
            /// <summary><see cref="T:System.String" />. A specified-length stream of (UTF-8) characters. The string is limited to about 1GB.</summary>
            public NpgsqlParameter Varchar { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Varchar; return p; } }
            /// <summary><see cref="T:System.String" />. A variable-length stream of (UTF-8) characters of unlimited length.</summary>
            public NpgsqlParameter Text { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Text; return p; } }

            /// <summary>Postgresql date type.</summary>
            public NpgsqlParameter Date { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Date; return p; } }
            /// <summary>Postgresql interval type.</summary>
            public NpgsqlParameter Interval { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Interval; return p; } }
            /// <summary>Postgresql time without time zone type.</summary>
            public NpgsqlParameter Time { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Time; return p; } }
            /// <summary>Postgresql timestamp without time zone type.</summary>
            public NpgsqlParameter Timestamp { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Timestamp; return p; } }
            /// <summary>Postgresql timestamp with time zone type.</summary>
            public NpgsqlParameter TimestampTz { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.TimestampTz; return p; } }
            /// <summary>Postgresql time with time zone type.</summary>
            public NpgsqlParameter TimeTz { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.TimeTz; return p; } }
            /// <summary>A string of 1's and 0's</summary>
            public NpgsqlParameter Bit { get { p.NpgsqlDbType = NpgsqlDbType.Range | NpgsqlDbType.Bit; return p; } }
        }

        #endregion

    }
}
