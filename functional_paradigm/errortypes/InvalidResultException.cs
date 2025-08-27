namespace contoso.functional;

/// <summary>Attempt to set the value or exception of <see cref="Result{T}"/> with a null reference.</summary>
[Serializable]
public class InvalidResultException : Exception
{
    const string _defaultErrMsg = "A Result cannot be initialized with a null success value or with a null exception.";

    ///<inheritdoc/>
    public InvalidResultException() : base(_defaultErrMsg) { }

    ///<inheritdoc/>
    public InvalidResultException(string message) : base(message) { }

    /// <summary>Conveys the attempt to set a successful <see cref="Result{T}"/> with a null value.</summary>
    public static InvalidResultException ForNullSuccessValue => new InvalidResultException("A success Result cannot be created with a null value");

    /// <summary>Conveys the attempt to set a failed <see cref="Result{T}"/> with a null <see cref="Exception"/>.</summary>
    public static InvalidResultException ForUninitializedException => new InvalidResultException($"A failed Result cannot be created from a null exception");
}
