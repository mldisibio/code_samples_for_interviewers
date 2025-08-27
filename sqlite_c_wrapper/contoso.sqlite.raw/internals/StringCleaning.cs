using System.Text.RegularExpressions;

namespace contoso.sqlite.raw.stringutils
{
	internal partial class StringCleaningPatterns
	{
		// find one or more consecutive newlines (LF or CRLF); leaves lone CR;
		[GeneratedRegex(@"(\r\n)+|\n+", RegexOptions.None, "en-US")]
		public static partial Regex OneOrMoreNewlines();

		// find one or more consecutive newlines or tabs (LF or CRLF or TAB); leaves lone CR;
		[GeneratedRegex(@"(\r\n)+|\n+|\t+", RegexOptions.None, "en-US")]
		public static partial Regex OneOrMoreTabsOrNewlines();

		// first 32 control characters (includes \r and \n) and 127(x7E) DEL
		[GeneratedRegex(@"[\u0000-\u001F\u007F]+", RegexOptions.None, "en-US")]
		public static partial Regex NotPrintableAscii();

		// find two or more consecutive space characters
		[GeneratedRegex(@"\u0020{2,}", RegexOptions.None, "en-US")]
		public static partial Regex TwoOrMoreSpaces();

		// per RFC 4180 [ DQUOTE (x22), COMMA (x2C), CR (x0D), LF (x0A),]; 
		// i've added single quote (x27) as this is problematic for postgres; i've added any control char (x00-x1F and x7F) in case they are preserved and affect line breaking;
		// sqlite escapes text with SPACE (0x20) but not included here; sqlite also escapes extended ascii (0x80-0xFF) but not included here
		//[GeneratedRegex(@"[\u0022\u0027\u002C\u0000-\u001F\u007F]", RegexOptions.None, "en-US")]
		//public static partial Regex CharsNeedingEscape();

		// find a DQUOTE (x22)["] so that, per RFC 4180, it can be escaped with another DQUOTE;
		// funky syntax matches 'one' double quote; using a redundant escape syntax
		[GeneratedRegex(@"\u0022", RegexOptions.None, "en-US")]
		public static partial Regex DblQuoteChar();
	}

	/// <summary>Extensions for cleaning text, flattening, and escaping for csv.</summary>
	/// <remarks>
	/// Since the primary use-case for these extensions is field by field cleaning and csv prep for large ammounts of data,
	/// they attempt to balance performance suggestions for not allocating unnecessary strings with not writing code that is obscure or ineffective.
	/// </remarks>
	public static class StringCleaning
	{
		// see https://github.com/dotnet/coreclr/pull/13219 for optimizations around submitting 2 or 3 characters only to IndexOfAny...
		// the idea is that if you only have 2 or 3 chars as an argument to IndexOfAny, it checks for them in one loop; anything more requires a loop per char;
		const char _dblQuote = '"';
		readonly static string _twoSpaces = "  ";
		readonly static char[] _newLineChars = new char[] { '\n', '\r' };
		readonly static char[] _newLineAndTabChars = new char[] { '\n', '\r', '\t' };
		// DblQuote, SingleQuote, Comma (to be combined with newLineChars)
		readonly static char[] _additionalEscapeCandidates = new char[] {(char)0x22, (char)0x27, (char)0x2C };
		// first 32 control characters except for \n\r\t; also includes 127 (DEL)
		readonly static char[] _additionalCtlChars = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 127, 0 }.Select(b => (char)b).ToArray();

		/// <summary>Replace one or more consecutive new lines (\n or \r\n) with one space. Optionally do same for \t.</summary>
		public static string? NewlinesToSpace(this string? src, bool includeTabs = false)
		{
			if (src != null)
			{
				if (includeTabs)
				{
					if (src.IndexOfAny(_newLineAndTabChars) > -1)
						return StringCleaningPatterns.OneOrMoreTabsOrNewlines().Replace(src, " ");
				}
				else if (src.IndexOfAny(_newLineChars) > -1)
					return StringCleaningPatterns.OneOrMoreNewlines().Replace(src, " ");
			}
			return src;
		}

		/// <summary>Remove non-printable ascii control characters (0-31, 127). This includes newlines and tab, so replace those first if you want to flatten with spacing.</summary>
		public static string? RemoveControlChars(this string? src)
			=> src != null && (src.IndexOfAny(_newLineAndTabChars) > -1 || src.IndexOfAny(_additionalCtlChars) > -1) ? StringCleaningPatterns.NotPrintableAscii().Replace(src, String.Empty) : src;

		/// <summary>Replace one or more consecutive spaces with one space.</summary>
		public static string? TrimConsecutiveSpaces(this string? src) => src != null && src.IndexOf(_twoSpaces, StringComparison.Ordinal) > -1 ? StringCleaningPatterns.TwoOrMoreSpaces().Replace(src, " ") : src;

		/// <summary>Since whitespace is significant in csv per RFC 4180, trim the csv field, but only allocate the new string if necessary.</summary>
		public static string? TrimCsvField(this string? src) => src != null && src.Length > 0 && (src[0] == ' ' || src[^1] == ' ') ? src.Trim() : src;

		/// <summary>Return <paramref name="cnt"/> characters from the start of <paramref name="src"/>, or <paramref name="src"/> if its length is less than <paramref name="cnt"/>.</summary>
		public static ReadOnlySpan<char> Left(this string? src, int cnt) => (src is null || src.Length <= cnt) ? src : src.AsSpan(0, cnt);

		/// <summary>Apply basic RFC 4180 rules for when to enclosing a csv field in double-quotes.</summary>
		/// <param name="src">The string to check for characters requiring it to be enclosed in double quotes.</param>
		/// <param name="checkForCtlChars">
		/// True to check for chars 0-32,127. If these are known to have been removed already, this can be left false (default).
		/// The assumption is that all non-binary-format data from generators is plain printable ascii
		/// and therefore control characters are unwanted and have been removed from <paramref name="src"/> prior to this step.
		/// </param>
		/// <param name="sep">The separator character, if not a comma.</param>
		public static string? EscapeForCsv(this string? src, bool checkForCtlChars = false, char sep = ',')
		{
			if (src == null || src.Length == 0)
				return src;
			// check if the text has one or more double quotation marks inside it
			bool hasDblQuote = src.IndexOf(_dblQuote, StringComparison.Ordinal) > -1;
			// check if text has chars requiring enclosure in double quotes
			bool mustEscape = hasDblQuote
						   || src.IndexOfAny(_additionalEscapeCandidates) > -1
						   || src.IndexOfAny(_newLineChars) > -1
						   || (sep != ',' && src.IndexOf(sep) > -1)
						   || (checkForCtlChars && src.IndexOfAny(_additionalCtlChars) > -1);
			// if escaping not needed, return the original string
			// if text had a double quotation mark (will already be tagged as requiring escaping), double each dbl-quote char (per RFC 4180)
			// escape the final string by enclosing it in double quotation marks
			return mustEscape
				   ? hasDblQuote ? $"\"{StringCleaningPatterns.DblQuoteChar().Replace(src, "\"\"")}\"" : $"\"{src}\""
				   : src;
		}

		/// <summary>Concatenate a collection of string. Returns an empty string if <paramref name="src"/> is null.</summary>
		/// <param name="src">A collection of strings to be transformed into a single delimited string.</param>
		/// <param name="delim">The delimiter. Can be multiple characters. Defaults to a comma without spaces.</param>
		/// <param name="toLower">True if all items should be converted to lower case. Default is to leave as-is.</param>
		/// <param name="preserveNulls">True to include null items as an empty string in the delimited list. False to remove them. Default is false.</param>
		public static string AsDelimitedString(this IEnumerable<string>? src, string delim = ",", bool toLower = false, bool preserveNulls = false)
		{
			if (src == null)
				return string.Empty;

			var items = src.Select(item => item.EmptyIfNullElseTrimmed());
			if (!preserveNulls)
				items = items.Where(item => !string.IsNullOrEmpty(item));
			if (toLower)
				items = items.Select(item => item.ToLower());
			return string.Join(delim, items);
		}
	}
}
