namespace contoso.functional
{

	public static partial class TaskContainer
	{
		/// <summary>Invoke <paramref name="factory"/> after awaiting <paramref name="task"/> and return a <see cref="Task{TResult}"/></summary>
		public static async Task<TResult> Map<TResult>(this Task task, Func<TResult> factory)
		{
			await task.ConfigureAwait(false);
			return factory();
		}

		/// <summary>
		/// Apply <paramref name="Completed"/> to the <typeparamref name="TSource"/> result from <paramref name="task"/> 
		/// or apply <paramref name="Faulted"/> to it's exception, and return a <see cref="Task{TResult}"/>.
		/// </summary>
		public static Task<TResult> Map<TSource, TResult>(this Task<TSource> task, Func<Exception, TResult> Faulted, Func<TSource, TResult> Completed)
			=> task.ContinueWith
			(
				prev => prev.Status == TaskStatus.Faulted
						? Faulted(prev.Exception!)
						: prev.Status == TaskStatus.Canceled
						  ? Faulted(new TaskCanceledException())
						  : Completed(prev.Result)
			);

		/// <summary>
		/// Apply <paramref name="Completed"/> to the <typeparamref name="TSource"/> result from <paramref name="task"/> 
		/// or apply <paramref name="Faulted"/> to it's exception, and return a <see cref="Task{TResult}"/>.
		/// </summary>
		public static Task<TResult> MapAsync<TSource, TResult>(this Task<TSource> task, Func<Exception, Task<TResult>> Faulted, Func<TSource, Task<TResult>> Completed)
			=> task.ContinueWith
			(
				prev => prev.Status == TaskStatus.Faulted
						? Faulted(prev.Exception!)
						: prev.Status == TaskStatus.Canceled
						  ? Faulted(new TaskCanceledException())
						  : Completed(prev.Result)
			)
			.Unwrap();

		/// <summary>Await <paramref name="task"/> and then return <paramref name="result"/> as awaitable. Completion status of <paramref name="task"/> is not checked.</summary>
		public static async Task<TResult> AndReturn<TResult>(this Task task, TResult result)
		{
			await task.ConfigureAwait(false);
			return result;
		}

		/// <summary>
		/// Await <paramref name="task"/> and return its Result. 
		/// Provide a <paramref name="fallback"/> implementation for <paramref name="task"/> if it completes in the Faulted (or Canceled) state.
		/// Use this when ignoring any (expected) exception such as a network timeout.
		/// </summary>
		public static Task<T> OrElse<T>(this Task<T> task, Func<Task<T>> fallback)
			=> task.ContinueWith
			(
				prev => prev.Status == TaskStatus.Faulted || prev.Status == TaskStatus.Canceled
						? fallback()
						: prev
			).Unwrap();

		/// <summary>Apply <paramref name="map"/> to the <typeparamref name="TSource"/> result from <paramref name="task"/> and return a <see cref="Task{TResult}"/></summary>
		public static async Task<TResult> Map<TSource, TResult>(this Task<TSource> task, Func<TSource, TResult> map)
			=> map(await task.ConfigureAwait(false));
	}
}


namespace contoso.functional.advanced
{

	public static partial class TaskContainerAdvanced
	{
		/// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="task"/> has been partially applied as the first argument to <paramref name="producer"/>.</summary>
		public static Task<Func<T2, TResult>> Map<T1, T2, TResult>(this Task<T1> task, Func<T1, T2, TResult> producer)
			=> task.Map(producer.Curry());

		/// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="task"/> has been partially applied as the first argument to <paramref name="producer"/>.</summary>
		public static Task<Func<T2, T3, TResult>> Map<T1, T2, T3, TResult>(this Task<T1> task, Func<T1, T2, T3, TResult> producer)
			=> task.Map(producer.CurryFirst());

		/// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="task"/> has been partially applied as the first argument to <paramref name="producer"/>.</summary>
		public static Task<Func<T2, T3, T4, TResult>> Map<T1, T2, T3, T4, TResult>(this Task<T1> task, Func<T1, T2, T3, T4, TResult> producer)
			=> task.Map(producer.CurryFirst());

		/// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="task"/> has been partially applied as the first argument to <paramref name="producer"/>.</summary>
		public static Task<Func<T2, T3, T4, T5, TResult>> Map<T1, T2, T3, T4, T5, TResult>(this Task<T1> task, Func<T1, T2, T3, T4, T5, TResult> producer)
			=> task.Map(producer.CurryFirst());

		/// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="task"/> has been partially applied as the first argument to <paramref name="producer"/>.</summary>
		public static Task<Func<T2, T3, T4, T5, T6, TResult>> Map<T1, T2, T3, T4, T5, T6, TResult>(this Task<T1> task, Func<T1, T2, T3, T4, T5, T6, TResult> producer)
			=> task.Map(producer.CurryFirst());

		/// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="task"/> has been partially applied as the first argument to <paramref name="producer"/>.</summary>
		public static Task<Func<T2, T3, T4, T5, T6, T7, TResult>> Map<T1, T2, T3, T4, T5, T6, T7, TResult>(this Task<T1> task, Func<T1, T2, T3, T4, T5, T6, T7, TResult> producer)
			=> task.Map(producer.CurryFirst());

		/// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="task"/> has been partially applied as the first argument to <paramref name="producer"/>.</summary>
		public static Task<Func<T2, T3, T4, T5, T6, T7, T8, TResult>> Map<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this Task<T1> task, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> producer)
			=> task.Map(producer.CurryFirst());

		/// <summary>Return a task wrapping the curried function expecting the remaining arguments after the result from <paramref name="task"/> has been partially applied as the first argument to <paramref name="producer"/>.</summary>
		public static Task<Func<T2, T3, T4, T5, T6, T7, T8, T9, TResult>> Map<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(this Task<T1> task, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> producer)
			=> task.Map(producer.CurryFirst());
	}
}