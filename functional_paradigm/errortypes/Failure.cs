using System.Text;
using contoso.utility.exceptionwrappers;

namespace contoso.functional;

/// <summary>Wraps the <see cref="Exception"/> and optional context from a <see cref="Result{T}"/> in the exception state.</summary>
public record Failure(Exception Exception, Option<string> Context, Option<string> CalledFrom)
{
	/// <inheritdoc/>
	protected virtual bool PrintMembers(StringBuilder builder)
	{
		builder.Append($"{Exception.ExtractDetail(CalledFrom.GetValueOr(() => null!))}")
			   //.Append("Exception = ")
			   //.Append($"[{Exception.GetType().Name}]: {Exception.Message.ReplaceWithSpace(",")}")
			   //.Append(", CalledFrom = ")
			   //.Append(CalledFrom.GetValueOr(string.Empty))
			   .Append(", Context = [")
			   .Append(Context.GetValueOr(string.Empty).ReplaceWithSpace(","))
			   .Append(']');
		return true;
	}
}