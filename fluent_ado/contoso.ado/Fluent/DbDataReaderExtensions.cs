using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace contoso.ado
{
    /// <summary>Helper methods for iterating and reading the <see cref="DbDataReader"/>.</summary>
    public static class DataReaderExtensions
    {
        /// <summary>Checks for null before calling GetValue.</summary>
        /// <returns>The boxed value of the column, or null if the column is <see cref="DBNull"/>.</returns>
        public static object? GetNullableItem(this DbDataReader row, int i)
        {
            object val;
            if ((val = row.GetValue(i)) == DBNull.Value)
                return null;
            return val;
        }

        /// <summary>Checks for null before calling GetValue.</summary>
        /// <returns>The boxed value of the column, or null if the column is <see cref="DBNull"/>.</returns>
        public static object? GetNullableItem(this DbDataReader row, string name)
        {
            object val;
            if ((val = row[name]) == DBNull.Value)
                return null;
            return val;
        }

        /// <summary>Checks for null before calling GetString.</summary>
        /// <returns>The string value of the column, or null if the column is <see cref="DBNull"/>.</returns>
        public static string? GetNullableString(this DbDataReader row, int i)
        {
            object val;
            if ((val = row.GetValue(i)) == DBNull.Value)
                return null;
            return val.ToString();
        }

        /// <summary>Checks for null before calling GetString.</summary>
        /// <returns>The string value of the column, or null if the column is <see cref="DBNull"/>.</returns>
        public static string? GetNullableString(this DbDataReader row, string name)
        {
            object val;
            if ((val = row[name]) == DBNull.Value)
                return null;
            return val.ToString();
        }

        /// <summary>Return a nullable value type from an IDataReader.</summary>
        public static Nullable<T> GetNullableValue<T>(this DbDataReader row, int i)
            where T : struct
        {
            object val;
            if ((val = row.GetValue(i)) == DBNull.Value)
                return null;
            return (T)val;
        }

        /// <summary>Return a nullable value type from an IDataReader.</summary>
        public static Nullable<T> GetNullableValue<T>(this DbDataReader row, string name)
            where T : struct
        {
            object val;
            if ((val = row[name]) == DBNull.Value)
                return null;
            return (T)val;
        }

        /// <summary>Convert a DataReader tinyint value to boolean.</summary>
        public static bool GetBooleanFromByte(this DbDataReader row, int i) => row.GetNullableValue<byte>(i).GetValueOrDefault() > 0;

        /// <summary>Check a boolean field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.Boolean"/>.</summary>
        public static bool? GetNullableBoolean(this DbDataReader row, int i) => row.IsDBNull(i) ? (bool?)null : row.GetBoolean(i);

        /// <summary>Check a byte field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.Byte"/>.</summary>
        public static byte? GetNullableByte(this DbDataReader row, int i) => row.IsDBNull(i) ? (byte?)null : row.GetByte(i);

        /// <summary>Check a char field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.Char"/>.</summary>
        public static char? GetNullableChar(this DbDataReader row, int i) => row.IsDBNull(i) ? (char?)null : row.GetChar(i);

        /// <summary>Check a DateTime field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.DateTime"/>.</summary>
        public static DateTime? GetNullableDateTime(this DbDataReader row, int i) => row.IsDBNull(i) ? (DateTime?)null : row.GetDateTime(i);

        /// <summary>Check a decimal field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.Decimal"/>.</summary>
        public static decimal? GetNullableDecimal(this DbDataReader row, int i) => row.IsDBNull(i) ? (decimal?)null : row.GetDecimal(i);

        /// <summary>Check a double field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.Double"/>.</summary>
        public static double? GetNullableDouble(this DbDataReader row, int i) => row.IsDBNull(i) ? (double?)null : row.GetDouble(i);

        /// <summary>Check a float field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.Single"/>.</summary>
        public static float? GetNullableFloat(this DbDataReader row, int i) => row.IsDBNull(i) ? (float?)null : row.GetFloat(i);

        /// <summary>Check a Guid field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.Guid"/>.</summary>
        public static Guid? GetNullableGuid(this DbDataReader row, int i) => row.IsDBNull(i) ? (Guid?)null : row.GetGuid(i);

        /// <summary>Check a short integer field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.Int16"/>.</summary>
        public static short? GetNullableInt16(this DbDataReader row, int i) => row.IsDBNull(i) ? (short?)null : row.GetInt16(i);

        /// <summary>Check an integer field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.Int32"/>.</summary>
        public static int? GetNullableInt32(this DbDataReader row, int i) => row.IsDBNull(i) ? (int?)null : row.GetInt32(i);

        /// <summary>Check a long integer field for <see cref="DBNull"/> and return its value as a nullable <see cref="System.Int64"/>.</summary>
        public static long? GetNullableInt64(this DbDataReader row, int i) => row.IsDBNull(i) ? (long?)null : row.GetInt64(i);

        /// <summary>
        /// Checks each element for null after calling GetValues.
        /// The supplied array is filled with the boxed values of the column, substituted with null if the column was <see cref="DBNull"/>.
        /// </summary>
        /// <returns>Count of columns.</returns>
        public static int GetNullableValues(this DbDataReader row, ref object[] values)
        {
            int fieldCount = row.GetValues(values);
            for (int i = 0; i < fieldCount; i++)
                if (values[i] == DBNull.Value)
                    values[i] = null!;
            return fieldCount;
        }

        /// <summary>
        /// Reads the schema table of an <see cref="IDataReader"/>, in which all .Net providers include 'ColumnName' and 'ColumnOrdinal' columns,
        /// and converts them to a Dictionary{String, Int32} with the property names as keys and the column ordinals as the values. 
        /// </summary>
        public static Dictionary<string, int> ToColumnDictionary(this DbDataReader reader)
        {
            var schemaTable = reader.GetSchemaTable();

            if (schemaTable == null || schemaTable.Rows == null)
                return new Dictionary<string, int>(0);
            else
            {
                int colName = schemaTable.Columns.IndexOf("ColumnName");
                int colOrd = schemaTable.Columns.IndexOf("ColumnOrdinal");
                return schemaTable.Rows.Cast<DataRow>().ToDictionary(row => (string)row[colName], row => (int)row[colOrd], StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
