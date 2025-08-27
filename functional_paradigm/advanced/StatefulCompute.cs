using Unit = System.ValueTuple;

namespace contoso.functional.advanced;
#pragma warning disable CS1591

/// <summary>Signature of an operation in which state is both passed in and returned.</summary>
/// <remarks>The stateful compute pattern allows state to be an explicit input and output of the function rather than allowing mutation of global state.</remarks>
public delegate (T Value, TState State) StatefulComputation<TState, T>(TState state);

public static class StatefulCompute<TState>
{
    public static StatefulComputation<TState, T> Return<T>(T value) => state => (value, state);

    public static StatefulComputation<TState, Unit> Put(TState newState) => state => (FnConstructs.Unit(), newState);

    public static StatefulComputation<TState, TState> Get => state => (state, state);
}

public static class StatefulCompute
{
    public static T Run<TState, T>(this StatefulComputation<TState, T> f, TState state)
        => f(state).Value;

    public static StatefulComputation<TState, Unit> Put<TState>(TState newState)
        => state => (FnConstructs.Unit(), newState);

    public static StatefulComputation<TState, TState> Get<TState>()
        => state => (state, state);

    public static StatefulComputation<TState, TResult> Select<TState, T, TResult>(this StatefulComputation<TState, T> statefulCompute, Func<T, TResult> project)
        => state0 =>
        {
            var (t, state1) = statefulCompute(state0);
            return (project(t), state1);
        };

    public static StatefulComputation<TState, TResult> SelectMany<TState, TSource, TResult>(this StatefulComputation<TState, TSource> statefulCompute,
                                                                                            Func<TSource, StatefulComputation<TState, TResult>> transform)
        => state0 =>
        {
            var (t, state1) = statefulCompute(state0);
            return transform(t)(state1);
        };

    public static StatefulComputation<TState, TResult> SelectMany<TState, TSource, TCollection, TResult>(this StatefulComputation<TState, TSource> statefulCompute,
                                                                                                         Func<TSource, StatefulComputation<TState, TCollection>> bind,
                                                                                                         Func<TSource, TCollection, TResult> project)
        => state0 =>
           {
               var (sourceT, state1) = statefulCompute(state0);
               var (item, state2) = bind(sourceT)(state1);
               var result = project(sourceT, item);
               return (result, state2);
           };

}

#pragma warning restore CS1591