using System;
using contoso.logfiles.common;
using contoso.utility.fluentextensions;

namespace contoso.decaf.wrapper.directory
{
    internal static class PrefixComposer
    {
        public static string ComposeDirectoryPrefix(this string fullPath, OutputPrefixOption prefixOption) 
        {
            if (prefixOption.Equals(OutputPrefixOption.None))
                return null;
            if (fullPath.IsNullOrEmptyString())
                return fullPath;
            var snFilter = new SerialNumberFilter(fullPath);
            if (snFilter.Success)
            {
                return prefixOption switch
                {
                    OutputPrefixOption.GeneratorYear => $"{snFilter.SerialNumber[0]}{snFilter.GeneratorYearText}",
                    OutputPrefixOption.GeneratorYearMonth => $"{snFilter.SerialNumber[0]}{snFilter.GeneratorYearText}{snFilter.GeneratorMonthText}",
                    _ => null
                };
            }
            return null;
        }
    }
}
