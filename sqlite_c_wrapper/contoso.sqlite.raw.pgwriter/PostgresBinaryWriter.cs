using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace contoso.sqlite.raw.pgwriter
{
    /// <summary>Wraps the Postgres binary writer such that callers do not need a direct dependency on the Npgsql library. Adds explicit strong typed extensions.</summary>
    public class PostgresBinaryWriter : IDisposable, IAsyncDisposable
    {
        readonly NpgsqlBinaryImporter _w;
        readonly CancellationToken _cancelToken;
        bool _isAlreadyDisposed;

        internal PostgresBinaryWriter(NpgsqlBinaryImporter writer, CancellationToken cancelToken = default)
        {
            _w = writer;
            _cancelToken = cancelToken;
        }

        /// <summary>Starts writing a single row, must be invoked before writing columns, unless using <see cref="WriteRow(object[])"/>.</summary>
        public PostgresBinaryWriter StartRow() { _w.StartRow(); return this; }
        /// <summary>Calls <see cref="StartRow"/> and writes an entire row of columns using Npgsql default type conversions.</summary>
        /// <param name="values">An array of column values, in write order, to be written as a single row.</param>
        public PostgresBinaryWriter WriteRow(params object[] values) { _w.WriteRow(values); return this; }
        /// <summary>Writes a single null column value.</summary>
        public PostgresBinaryWriter WriteNull() { _w.WriteNull(); return this; }

        /// <summary>Writes <paramref name="fld"/> or null to a Postgres boolean column.</summary>
        public PostgresBinaryWriter Write(bool? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Boolean); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres smallint column.</summary>
        public PostgresBinaryWriter Write(byte? fld) { if (fld.HasValue) { _w.Write((short)fld.Value, NpgsqlDbType.Smallint); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres smallint column.</summary>
        public PostgresBinaryWriter Write(sbyte? fld) { if (fld.HasValue) { _w.Write((short)fld.Value, NpgsqlDbType.Smallint); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres smallint column.</summary>
        public PostgresBinaryWriter Write(short? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Smallint); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres integer column.</summary>
        public PostgresBinaryWriter Write(ushort? fld) { if (fld.HasValue) { _w.Write((int)fld.Value, NpgsqlDbType.Integer); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres integer column.</summary>
        public PostgresBinaryWriter Write(int? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Integer); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres bigint column.</summary>
        public PostgresBinaryWriter Write(uint? fld) { if (fld.HasValue) { _w.Write((long)fld.Value, NpgsqlDbType.Bigint); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres bigint column.</summary>
        public PostgresBinaryWriter Write(long? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Bigint); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres uuid column.</summary>
        public PostgresBinaryWriter Write(Guid? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Uuid); } else { _w.WriteNull(); } return this; }

        /// <summary>Writes <paramref name="fld"/> or null to a Postgres real column.</summary>
        public PostgresBinaryWriter Write(float? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Real); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres double column.</summary>
        public PostgresBinaryWriter Write(double? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Double); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres numeric column.</summary>
        public PostgresBinaryWriter Write(decimal? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Numeric); } else { _w.WriteNull(); } return this; }

        /// <summary>Writes <paramref name="fld"/> or null to a Postgres text column.</summary>
        public PostgresBinaryWriter WriteText(string? fld) { if (fld != null) { _w.Write(fld, NpgsqlDbType.Text); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres varchar column.</summary>
        public PostgresBinaryWriter WriteVarchar(string? fld) { if (fld != null) { _w.Write(fld, NpgsqlDbType.Varchar); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres char column.</summary>
        public PostgresBinaryWriter WriteChar(string? fld) { if (fld != null) { _w.Write(fld, NpgsqlDbType.Char); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres bytea column.</summary>
        public PostgresBinaryWriter WriteBytes(byte[]? fld) { if (fld.IsNotNullOrEmpty()) { _w.Write(fld, NpgsqlDbType.Bytea); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres bytea column.</summary>
        public PostgresBinaryWriter WriteBytes(ReadOnlySpan<byte> fld) { if (fld.Length > 0) { _w.Write(fld.ToArray(), NpgsqlDbType.Bytea); } else { _w.WriteNull(); } return this; }

        /// <summary>Writes <paramref name="fld"/> or null to a Postgres timestamp column.</summary>
        public PostgresBinaryWriter WriteTimestamp(DateTime? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Timestamp); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres timestamp with time zone column.</summary>
        public PostgresBinaryWriter WriteTimestampTz(DateTime? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.TimestampTz); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres date column.</summary>
        public PostgresBinaryWriter WriteDate(DateTime? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Date); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres time column.</summary>
        public PostgresBinaryWriter WriteTime(TimeSpan? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Time); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres time with time zone column.</summary>
        public PostgresBinaryWriter WriteTimeTz(DateTimeOffset? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.TimeTz); } else { _w.WriteNull(); } return this; }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres interval column.</summary>
        public PostgresBinaryWriter WriteInterval(TimeSpan? fld) { if (fld.HasValue) { _w.Write(fld.Value, NpgsqlDbType.Interval); } else { _w.WriteNull(); } return this; }

        /// <summary>Starts writing a single row, must be invoked before writing columns, unless using <see cref="WriteRowAsync(object[])"/>.</summary>
        public Task StartRowAsync() => _w.StartRowAsync(_cancelToken);
        /// <summary>Calls <see cref="StartRowAsync"/> and writes an entire row of columns using Npgsql default type conversions.</summary>
        /// <param name="values">An array of column values, in write order, to be written as a single row.</param>
        public Task WriteRowAsync(params object[] values) => _w.WriteRowAsync(_cancelToken, values);
        /// <summary>Writes a single null column value.</summary>
        public Task WriteNullAsync() => _w.WriteNullAsync();

        /// <summary>Writes <paramref name="fld"/> or null to a Postgres boolean column.</summary>
        public async Task WriteAsync(bool? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Boolean).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres smallint column.</summary>
        public async Task WriteAsync(byte? fld) { if (fld.HasValue) { await _w.WriteAsync((short)fld.Value, NpgsqlDbType.Smallint).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres smallint column.</summary>
        public async Task WriteAsync(sbyte? fld) { if (fld.HasValue) { await _w.WriteAsync((short)fld.Value, NpgsqlDbType.Smallint).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres smallint column.</summary>
        public async Task WriteAsync(short? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Smallint).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres integer column.</summary>
        public async Task WriteAsync(ushort? fld) { if (fld.HasValue) { await _w.WriteAsync((int)fld.Value, NpgsqlDbType.Integer).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres integer column.</summary>
        public async Task WriteAsync(int? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Integer).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres bigint column.</summary>
        public async Task WriteAsync(uint? fld) { if (fld.HasValue) { await _w.WriteAsync((long)fld.Value, NpgsqlDbType.Bigint).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres bigint column.</summary>
        public async Task WriteAsync(long? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Bigint).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres uuid column.</summary>
        public async Task WriteAsync(Guid? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Uuid).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }

        /// <summary>Writes <paramref name="fld"/> or null to a Postgres real column.</summary>
        public async Task WriteAsync(float? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Real).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres double column.</summary>
        public async Task WriteAsync(double? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Double).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres numeric column.</summary>
        public async Task WriteAsync(decimal? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Numeric).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }

        /// <summary>Writes <paramref name="fld"/> or null to a Postgres text column.</summary>
        public async Task WriteTextAsync(string? fld) { if (fld != null) { await _w.WriteAsync(fld, NpgsqlDbType.Text).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres varchar column.</summary>
        public async Task WriteVarcharAsync(string? fld) { if (fld != null) { await _w.WriteAsync(fld, NpgsqlDbType.Varchar).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres char column.</summary>
        public async Task WriteCharAsync(string? fld) { if (fld != null) { await _w.WriteAsync(fld, NpgsqlDbType.Char).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres bytea column.</summary>
        public async Task WriteBytesAsync(byte[]? fld) { if (fld.IsNotNullOrEmpty()) { await _w.WriteAsync(fld, NpgsqlDbType.Bytea).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }

        /// <summary>Writes <paramref name="fld"/> or null to a Postgres timestamp column.</summary>
        public async Task WriteTimestampAsync(DateTime? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Timestamp).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres timestamp with time zone column.</summary>
        public async Task WriteTimestampTzAsync(DateTime? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.TimestampTz).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres date column.</summary>
        public async Task WriteDateAsync(DateTime? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Date).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres time column.</summary>
        public async Task WriteTimeAsync(TimeSpan? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Time).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres time with time zone column.</summary>
        public async Task WriteTimeTzAsync(DateTimeOffset? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.TimeTz).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }
        /// <summary>Writes <paramref name="fld"/> or null to a Postgres interval column.</summary>
        public async Task WriteIntervalAsync(TimeSpan? fld) { if (fld.HasValue) { await _w.WriteAsync(fld.Value, NpgsqlDbType.Interval).ConfigureAwait(false); } else { await _w.WriteNullAsync().ConfigureAwait(false); } }

        #region IDisposable 

        /// <summary>Cancels the binary import and sets the connection back to idle state.</summary>
        protected virtual void Dispose(bool isManagedCall)
        {
            if (!_isAlreadyDisposed)
            {
                _isAlreadyDisposed = true;

                if (isManagedCall)
                {
                    try { _w?.Dispose(); }
                    catch { }
                }
            }
        }

        /// <summary>Cancels the binary import and sets the connection back to idle state.</summary>
        protected virtual async ValueTask DisposeAsync(bool isManagedCall)
        {
            if (!_isAlreadyDisposed)
            {
                _isAlreadyDisposed = true;

                if (isManagedCall)
                {
                    if (_w != null)
                    {
                        try { await _w.DisposeAsync().ConfigureAwait(false); }
                        catch { }
                    }
                }
            }
        }

        /// <summary>Cancels the binary import and sets the connection back to idle state.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Cancels the binary import and sets the connection back to idle state.</summary>
        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return DisposeAsync(true);
        }

        #endregion


    }
}
