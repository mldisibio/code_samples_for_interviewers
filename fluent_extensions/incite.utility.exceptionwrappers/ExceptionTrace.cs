using System.Collections.Immutable;
using System.Text;

namespace contoso.utility.exceptionwrappers;
/// <summary>
/// Wraps an exception and any inner exceptions recursively extracted from it into a collection of <see cref="ExceptionDetail"/>
/// conveying essential information about the primary exception in a serialization friendly type.
/// </summary>
public record ExceptionTrace(ImmutableList<ExceptionDetail> Details, string? MethodName = null)
{
	/// <summary>Invoke <paramref name="format"/> to produce a formatted string of the exception detail.</summary>
	public string Format(Func<ExceptionTrace, string> format)
		=> format is null
		   ? ToString()
		   : format(this) ?? string.Empty;

	/// <inheritdoc/>
	protected virtual bool PrintMembers(StringBuilder builder)
	{
		builder.Append("MethodName = ")
			   .Append(MethodName ?? string.Empty);
		FormatDetails(builder, Details);
		return true;
	}

	static StringBuilder FormatDetails(StringBuilder builder, IEnumerable<ExceptionDetail> details)
		=> details.Count() > 1
		   ? builder.AppendLine(", ExceptionDetail = [")
					.Append(string.Join(Environment.NewLine, details.Select(d => d.ToString())))
					.Append(']')
		   : builder.Append(", ExceptionDetail = [")
					.Append(string.Join(Environment.NewLine, details.Select(d => d.ToString())))
					.Append(']');
}
