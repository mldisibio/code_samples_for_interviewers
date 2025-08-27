namespace contoso.decaf.wrapper
{
    /// <summary>Option for a prefix/parent directory to each serial number output directory.</summary>
    public enum OutputPrefixOption
    {
        /// <summary>No prefix. Files placed in serial number directory directly below specified output directory.</summary>
        None = 0,
        /// <summary>Files placed in serial number directory grouped by generator year (e.g. 'T5' below a specified output directory.</summary>
        GeneratorYear,
        /// <summary>Files placed in serial number directory grouped by generator year and month (e.g. 'T5A' below a specified output directory.</summary>
        GeneratorYearMonth
    }
}
