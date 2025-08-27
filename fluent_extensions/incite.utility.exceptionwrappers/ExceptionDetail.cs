using System.Collections.Immutable;
using System.Text;

namespace contoso.utility.exceptionwrappers;

/// <summary>Serialization friendly exception POCO.</summary>
public record struct ExceptionDetail
{
	const string _systemTrace = "at System.";
	const string _endTrace = "End of stack trace";

	string _type;
	string _msg;
	ImmutableList<string> _stack;

	ExceptionDetail(string type, string msg, ImmutableList<string> stack)
	{
		_type = type;
		_msg = msg;
		_stack = stack;
	}

	/// <summary>Full name of the exception type.</summary>
	public string Type
	{
		get => _type ??= "[Unknown]";
		init => _type = value;
	}

	/// <summary>Exception message.</summary>
	public string Msg
	{
		get => _msg ??= string.Empty;
		init => _msg = value;
	}

	/// <summary>A collection of individual stack trace lines.</summary>
	public ImmutableList<string> Stack
	{
		get => _stack ??= ImmutableList<string>.Empty;
		init => _stack = value;
	}

	/// <summary>Invoke <paramref name="format"/> to produce a formatted string of the exception detail.</summary>
	public string Format(Func<ExceptionDetail, string> format)
		=> format is null
		   ? ToString()
		   : format(this) ?? string.Empty;

	/// <summary>Implicit conversion from <see cref="Exception"/> if not null.</summary>
	/// <remarks>This is the preferred way to create an <see cref="ExceptionDetail"/> from an <see cref="Exception"/>.</remarks>
	public static implicit operator ExceptionDetail(Exception ex)
		=> ex is null
		   ? throw new ArgumentNullException(nameof(ex))
		   : ex is AggregateException aggEx
			 ? new ExceptionDetail
			   (
				   type: aggEx.GetType().FullName!,
				   msg: string.Join(" | ", aggEx.InnerExceptions.Select(e => e.Message)),
				   stack: aggEx.InnerExceptions.SelectMany(e => ParseStackTrace(e.StackTrace)).ToImmutableList()
			   )
			 : new ExceptionDetail
			   (
				   type: ex.GetType().FullName!,
				   msg: ex.Message,
				   stack: ParseStackTrace(ex.StackTrace).ToImmutableList()
			   );

	/// <summary>Implicit conversion from <see cref="AggregateException"/> if not null.</summary>
	public static implicit operator ExceptionDetail(AggregateException ex)
		=> ex is null
		   ? throw new ArgumentNullException(nameof(ex))
		   : new ExceptionDetail
		   (
			   type: ex.GetType().FullName!,
			   msg: string.Join(" | ", ex.InnerExceptions.Select(e => e.Message)),
			   stack: ex.InnerExceptions.SelectMany(e => ParseStackTrace(e.StackTrace)).ToImmutableList()
		   );

	// Convert the stack trace into a less noisy collection of lines, skipping the System.* lines
	static IEnumerable<string> ParseStackTrace(string? stackTrace)
	{
		if (stackTrace == null)
			yield break;

		var lines = stackTrace.Split(Environment.NewLine);
		foreach (ReadOnlySpan<char> line in lines)
		{
			if (line.IsEmpty)
				yield break;
			if (line.Contains(_systemTrace, StringComparison.Ordinal))
				yield break;
			if (line.Contains(_endTrace, StringComparison.Ordinal))
				yield break;

			yield return line.Trim().ToString();
		}
	}

	public override string ToString()
	{
		var builder = new StringBuilder();
		PrintMembers(builder);
		return builder.ToString();
	}

	bool PrintMembers(StringBuilder builder)
	{
		builder.Append("Type = ")
			   .Append(Type)
			   .Append(", Msg = ")
			   .Append(Msg.ReplaceWithSpace(","));
		FormatStack(builder, Stack);
		return true;

		static StringBuilder FormatStack(StringBuilder sb, IEnumerable<string> stack)
			=> stack.Count() > 2
			   ? sb.AppendLine()
				   .AppendLine(", Stack = [")
				   .Append(string.Join(Environment.NewLine, stack.Select(FormatStackLine)))
				   .Append(']')
			   : sb.Append(", Stack = [")
					.Append(string.Join(", ", stack.Select(FormatStackLine)))
					.Append(']');

		static string FormatStackLine(string s) => string.IsNullOrEmpty(s) ? s : s.ReplaceWithSpace(","); //.Elided(16);
	}
}
