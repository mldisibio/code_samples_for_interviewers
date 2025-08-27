using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace contoso.utility.exceptionwrappers;
#pragma warning disable CS1591

public static class ExceptionExtractor
{
	/// <summary>Compose a <see cref="ExceptionTrace"/> from <paramref name="ex"/>.</summary>
	public static ExceptionTrace ExtractDetail(this Exception ex, [CallerMemberName] string? methodName = "")
	{
		if (ex == null)
			return new ExceptionTrace(ImmutableList<ExceptionDetail>.Empty, methodName);

		// Fast-track for single, non-AggregateException
		if (ex is not AggregateException && ex.InnerException == null)
			return new ExceptionTrace(ImmutableList.Create((ExceptionDetail)ex), methodName);

		// ex will have at least one inner exception
		var orderedExceptions = FlattenInners(ex)
								.Reverse()
								.Select(ex => (ExceptionDetail)ex)
								.ToImmutableList();
		return new ExceptionTrace(orderedExceptions, methodName);
	}

	/// <summary>Returns <paramref name="ex"/> and any recursively extracted inner exceptions as a flat list.</summary>
	static IEnumerable<Exception> FlattenInners(Exception ex)
	{
		if (ex is AggregateException aggEx)
		{
			var flattenedAg = aggEx.Flatten().InnerExceptions.SelectMany(FlattenInners);
			foreach (var e in flattenedAg)
				yield return e;
		}
		else
		{
			yield return ex;

			if (ex.InnerException != null)
			{
				foreach (var e in FlattenInners(ex.InnerException))
					yield return e;
			}
		}
	}

	/// <summary>Replace <paramref name="sep"/> with a blank space.</summary>
	internal static string ReplaceWithSpace(this string s, string sep) => string.IsNullOrEmpty(s) ? s : s.Replace(sep, " ");
}
#pragma warning restore CS1591