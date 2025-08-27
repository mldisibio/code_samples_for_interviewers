using System;

namespace contoso.sqlite.raw
{
    /// <summary>Wraps the index for a specific parameter within a given sqlite prepared statement.</summary>
    /// <remarks>Primarily for supporting a fluent parameter binding syntax.</remarks>
    public class ParameterContext : OperationContext
    {
        /// <summary>Initialize with the one-based sql parameter index, and the current operation context.</summary>
        internal ParameterContext(int paramIdx, OperationContext ctx)
            : base(ctx)
        {
            Index = paramIdx;
        }

        /// <summary>The one-based parameter index within <see cref="OperationContext.Statement"/>.</summary>
        public int Index { get; }
    }

    /// <summary>Fluent methods for binding strongly typed values to a prepared statement.</summary>
    /// <remarks>Credit to Frank Krueger's sqlite-net code base.</remarks>
    public static class ParameterContextExtensions
    {
        /// <summary>Bind a parameter to a <see cref="Boolean"/> value using Int32 affinity.</summary>
        public static void ToBoolean(this ParameterContext ctx, bool? val) => ToInt32(ctx, val.HasValue ? val.Value ? 1 : 0 : (int?)null);
        /// <summary>Bind a parameter to a <see cref="Byte"/> value using Int32 affinity.</summary>
        public static void ToByte(this ParameterContext ctx, byte? val) => ToInt32(ctx, val ?? (int?)null);
        /// <summary>Bind a parameter to a <see cref="SByte"/> value using Int32 affinity.</summary>
        public static void ToSByte(this ParameterContext ctx, sbyte? val) => ToInt32(ctx, val ?? (int?)null);
        /// <summary>Bind a parameter to a <see cref="Int16"/> value using Int32 affinity.</summary>
        public static void ToInt16(this ParameterContext ctx, short? val) => ToInt32(ctx, val ?? (int?)null);
        /// <summary>Bind a parameter to a <see cref="UInt16"/> value using Int32 affinity.</summary>
        public static void ToUInt16(this ParameterContext ctx, ushort? val) => ToInt32(ctx, val ?? (int?)null);

        /// <summary>Bind a parameter to a <see cref="Int32"/> value using Int32 affinity.</summary>
        public static void ToInt32(this ParameterContext ctx, int? val)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int result;
            if (val.HasValue)
                result = SQLitePCL.raw.sqlite3_bind_int(ctx.Statement, ctx.Index, val.Value);
            else
                result = SQLitePCL.raw.sqlite3_bind_null(ctx.Statement, ctx.Index);
            ThrowOnFailure(result, ctx);
        }

        /// <summary>Bind a parameter to a <see cref="UInt32"/> value using Int64 affinity.</summary>
        public static void ToUInt32(this ParameterContext ctx, uint? val) => ToInt64(ctx, val ?? (long?)null);

        /// <summary>Bind a parameter to a <see cref="Int64"/> value using Int64 affinity.</summary>
        public static void ToInt64(this ParameterContext ctx, long? val)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int result;
            if (val.HasValue)
                result = SQLitePCL.raw.sqlite3_bind_int64(ctx.Statement, ctx.Index, val.Value);
            else
                result = SQLitePCL.raw.sqlite3_bind_null(ctx.Statement, ctx.Index);
            ThrowOnFailure(result, ctx);
        }

        /// <summary>Bind a parameter to a <see cref="Single"/> value using Double affinity.</summary>
        public static void ToSingle(this ParameterContext ctx, float? val) => ToDouble(ctx, val ?? (double?)null);
        /// <summary>Bind a parameter to a <see cref="Decimal"/> value using Double affinity.</summary>
        public static void ToDecimal(this ParameterContext ctx, decimal? val) => ToDouble(ctx, val.HasValue ? (double)val.Value : (double?)null);

        /// <summary>Bind a parameter to a <see cref="Double"/> value using Double affinity.</summary>
        public static void ToDouble(this ParameterContext ctx, double? val)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int result;
            if (val.HasValue)
                result = SQLitePCL.raw.sqlite3_bind_double(ctx.Statement, ctx.Index, val.Value);
            else
                result = SQLitePCL.raw.sqlite3_bind_null(ctx.Statement, ctx.Index);
            ThrowOnFailure(result, ctx);
        }

        /// <summary>Bind a parameter to the text representation of a <see cref="Guid"/> value using text affinity.</summary>
        public static void ToGuid(this ParameterContext ctx, Guid? val) => ToText(ctx, val.HasValue ? val.ToString() : null);
        /// <summary>Bind a parameter to the text representation of a <see cref="Uri"/> value using text affinity.</summary>
        public static void ToUri(this ParameterContext ctx, Uri val) => ToText(ctx, val?.ToString() == null ? null : val.ToString());

        /// <summary>Bind a parameter to a <see cref="String"/> value using text affinity.</summary>
        public static void ToText(this ParameterContext ctx, string? val)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int result;
            if (val == null)
                result = SQLitePCL.raw.sqlite3_bind_null(ctx.Statement, ctx.Index);
            else
                result = SQLitePCL.raw.sqlite3_bind_text(ctx.Statement, ctx.Index, val);
            ThrowOnFailure(result, ctx);
        }

        /// <summary>Bind a parameter to the characters referenced by a specific allocation of memory to be stored as sqlite text.</summary>
        public static void ToText(this ParameterContext ctx, ReadOnlySpan<byte> val)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int result;
            if (val == null)
                result = SQLitePCL.raw.sqlite3_bind_null(ctx.Statement, ctx.Index);
            else
                result = SQLitePCL.raw.sqlite3_bind_text(ctx.Statement, ctx.Index, val);
            ThrowOnFailure(result, ctx);
        }

        /// <summary>Bind a parameter to a byte array to be stored as a sqlite blob.</summary>
        public static void ToBlob(this ParameterContext ctx, byte[] val) => ToBlob(ctx, val.AsSpan());

        /// <summary>Bind a parameter to a byte array referenced by a specific allocation of memory to be stored as a sqlite blob.</summary>
        public static void ToBlob(this ParameterContext ctx, ReadOnlySpan<byte> val)
        {
            ctx.Statement.ThrowIfInvalid(ctx.FilePath);

            int result;
            if (val == null)
                result = SQLitePCL.raw.sqlite3_bind_null(ctx.Statement, ctx.Index);
            else
                result = SQLitePCL.raw.sqlite3_bind_blob(ctx.Statement, ctx.Index, val);
            ThrowOnFailure(result, ctx);
        }

        static void ThrowOnFailure(int resultCode, ParameterContext? ctx)
        {
            if (resultCode != SQLitePCL.raw.SQLITE_OK)
            {
                string errMsg = ctx?.DbHandle.SeemsValid() == true
                               ? SqliteDatabase.TryRetrieveError(ctx.DbHandle, resultCode)
                               : $"{resultCode}-{ResultCodes.Lookup[resultCode]}";
                throw new SqliteDbException(result: resultCode, msg: $"When setting parameter[{ctx?.Index}] value: {errMsg}", filePath: ctx?.FilePath);
            }
        }

        /*
            https://stackoverflow.com/a/8499544/458354 
            Your options to store UInt64 are:
                Store it as a string, converting as needed.
                Store it as a binary blob, converting as needed.
                Pretend that it is a signed 64-bit integer with a cast, thus converting as necessary.
                Store two pieces of information as two columns: the unsigned 63-bit integer (the lower 63-bits), and a value that represents the sign bit.
        */
    }
}
