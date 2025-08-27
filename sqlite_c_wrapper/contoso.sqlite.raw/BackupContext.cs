using System;
using System.IO;

namespace contoso.sqlite.raw
{
    /// <summary>Configure the single source schema (database) which will be backed up.</summary>
    public class BackupContextFrom
    {
        internal BackupContextFrom(ConnectionContext connection) => Connection = connection;

        internal ConnectionContext Connection { get; }

        internal string SourceSchemaName { get; private set; } = default!;

        /// <summary>Define the single source schema (database) which will be backed up.</summary>
        /// <param name="sourceSchemaName">
        /// This will be "main" for the main database, "temp" for the temporary database, or the name specified after the AS keyword in an ATTACH statement for an attached database.
        /// </param>
        public BackupContextTo From(string sourceSchemaName = "main")
        {
            SourceSchemaName = sourceSchemaName ?? "main";
            return new BackupContextTo(this);
        }
    }

    /// <summary>Configure the file path and target schema (database) of the backup file.</summary>
    public class BackupContextTo
    {
        readonly BackupContextFrom _bkupFrom;

        internal BackupContextTo(BackupContextFrom bkupFrom) => _bkupFrom = bkupFrom;

        internal string? TargetFilePath { get; private set; }

        internal string? TargetSchemaName { get; private set; }

        /// <summary>Define the file path and target schema (database) of the backup file.</summary>
        /// <param name="targetFilePath">Path on disk to where the backup will be written. This file will be overwritten if it already exists!</param>
        /// <param name="targetSchemaName">
        /// This will be "main" for the main database, "temp" for the temporary database, or the name specified after the AS keyword in an ATTACH statement for an attached database.
        /// </param>
        public void To(string targetFilePath, string targetSchemaName = "main")
        {
            TargetFilePath = Path.GetFullPath(targetFilePath);
            TargetSchemaName = targetSchemaName ?? "main";
            var bkupCtx = new BackupContext(_bkupFrom, this);
            // execute the backup; any exceptions will be bubbled up;
            bkupCtx.Execute();
        }
    }

    internal class BackupContext
    {
        readonly BackupContextFrom _bkupFrom;
        readonly BackupContextTo _bkupTo;

        internal BackupContext(BackupContextFrom bkupFrom, BackupContextTo bkupTo)
        {
            _bkupFrom = bkupFrom;
            _bkupTo = bkupTo;
        }

        /// <summary>Execute the backup following the recommended sqlite3 backup api.</summary>
        public void Execute()
        {
            _bkupFrom.Connection.DbHandle.ThrowIfInvalid();

            // create handle to backup destination, and close/dispose when backup is finished
            using (SqliteDatabase bkupToDb = SqliteDatabase.OpenForBackup(_bkupTo.TargetFilePath!))
            {
                WriteConnectionContext targetCtx = bkupToDb.GetOpenedWriteContext();
                // lock destination database for duration of backup operation
                targetCtx.InUseLock.Wait();
                _bkupFrom.Connection.InUseLock.Wait();
                try
                {
                    SQLitePCL.sqlite3_backup? bkupHandle = default;
                    try
                    {
                        _bkupFrom.Connection.StartTicks();
                        // initialize the backup; returns null on failure; any error message is actually stored in destination handle
                        bkupHandle = SQLitePCL.raw.sqlite3_backup_init(targetCtx.DbHandle, _bkupTo.TargetSchemaName, _bkupFrom.Connection.DbHandle, _bkupFrom.SourceSchemaName);
                        bool initFailed = (bkupHandle == null || bkupHandle == default);
                        if (!initFailed)
                            // copy all pages (negative page count means 'all'); any error message is actually stored in destination handle
                            SQLitePCL.raw.sqlite3_backup_step(bkupHandle, -1);
                    }
                    finally
                    {
                        // release all resources held by the 'backup' object (which is not the same as the target handle, which remains open; 
                        // any error message is actually stored in destination handle
                        bool neverCreated = (bkupHandle == null || bkupHandle == default);
                        if (!neverCreated)
                            SQLitePCL.raw.sqlite3_backup_finish(bkupHandle);
                    }

                    long? elapsed = _bkupFrom.Connection.GetElapsed();


                    int? result = targetCtx.DbHandle.SeemsValid() ? SQLitePCL.raw.sqlite3_errcode(targetCtx.DbHandle) : (int?)null;
                    if (result.HasValue && result.Value != SQLitePCL.raw.SQLITE_OK)
                    {
                        string errMsg = SqliteDatabase.TryRetrieveError(targetCtx.DbHandle, result);
                        var bkupEx = new SqliteDbException(result: result, msg: errMsg, filePath: _bkupTo.TargetFilePath);
                        SqliteDatabase.LogQueue.CommandFailed("BACKUP", bkupEx);
                        throw bkupEx;
                    }
                    SqliteDatabase.LogQueue.CommandExecuted($"BACKUP TO [{_bkupTo.TargetFilePath}]", elapsed);
                }
                finally
                {
                    // release db locks
                    _bkupFrom.Connection.InUseLock.Release();
                    targetCtx.InUseLock.Release();
                }
                // run a VACUUM on the target database
                // i never could get the all-in-one 'VACUUM INTO' to actually work, for reasons not yet discovered
                targetCtx.Execute("VACUUM;");
            } // dispose target handle
        }
    }
}
