namespace contoso.utility.compression;

/// <summary>Attempt to dereference a null object that I certainly did not expect to be null.</summary>
public class UnexpectedNullReferenceException : Exception
{
    const string _defaultErrMsg = "Dereferenced a null object. This was unexpected!";

    /// <summary>The default instance of <see cref="UnexpectedNullReferenceException"/>.</summary>
    public UnexpectedNullReferenceException() : base(_defaultErrMsg) { }

    /// <summary>Initialize a new instance of <see cref="UnexpectedNullReferenceException"/> with a specified error message.</summary>
    public UnexpectedNullReferenceException(string message) : base(message) { }
}

