namespace contoso.utility.compression;

/// <summary>Capture the input configuration state for diagnostics or logging.</summary>
public readonly record struct DecompressInputState(string? InputFile,
                                                   string? OutputFile,
                                                   bool InputIsStream,
                                                   bool OutputIsStream,
                                                   string? Header,
                                                   string? Footer);
