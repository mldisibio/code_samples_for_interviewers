using System;
using System.Data.Common;
using contoso.ado.Internals;

namespace contoso.ado.Common
{
    /// <summary>Factory class facilitating creation of an unconfigured <see cref="DbParameter"/>.</summary>
    public sealed class ParameterContext
    {
        /// <summary>The <see cref="Database"/> context for this instance.</summary>
        readonly Database _dbContext;

        /// <summary>Initialize with the given <see cref="Database"/> in context.</summary>
        internal ParameterContext(Database db)
        {
            _dbContext = db.ThrowIfNull();
        }

        /// <summary>Creates a new instance of a <see cref="DbParameter"/> object.</summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>An unconfigured parameter.</returns>
        public DbParameter CreateParameter(string parameterName)
        {
            return _dbContext.CreateParameter(parameterName);
        }
    }
}
