using SQLitePCL;

namespace contoso.sqlite.raw
{
    /// <summary>Low-level api requiring use of SQLitePCL.raw to register a user-defined function.</summary>
    public class RegisterFunctionContext
    {
        const int SQLITE_DIRECTONLY = 0x000080000; // https://sqlite.org/c3ref/c_deterministic.html#sqlitedirectonly
        const int _defaultFlags = SQLitePCL.raw.SQLITE_UTF8 | SQLitePCL.raw.SQLITE_DETERMINISTIC | SQLITE_DIRECTONLY;
        readonly SqliteContext _apiContext;

        internal RegisterFunctionContext(SqliteContext sqliteContext)
        {
            _apiContext = sqliteContext;
        }

        /// <summary>See https://sqlite.org/c3ref/create_function.html </summary>
        public void ScalarFunction(delegate_function_scalar scalarFunction, string functionName, int numArgs = -1, int preferredTextEnc = _defaultFlags)
        {
            if (functionName.IsNullOrEmptyString())
                throw new ArgumentNullException(nameof(functionName));
            if (numArgs < -1)
                throw new ArgumentOutOfRangeException(nameof(numArgs), "Number of arguments must be -1 or greater");
            ArgumentNullException.ThrowIfNull(scalarFunction);

            int result = SQLitePCL.raw.sqlite3_create_function(_apiContext.DbHandle, functionName, numArgs, preferredTextEnc, IntPtr.Zero, scalarFunction);
            if (result != SQLitePCL.raw.SQLITE_OK)
            {
                string errMsg = $"{SqliteDatabase.TryRetrieveError(_apiContext.DbHandle, result)} from 'sqlite3_create_function'";
                var regFnEx = new SqliteDbException(result: result, msg: errMsg, filePath: _apiContext.FilePath);
                SqliteDatabase.LogQueue.CommandFailed(functionName, regFnEx);
                throw regFnEx;
            }
        }
    }
}
