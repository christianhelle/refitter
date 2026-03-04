namespace Refitter.Core;

/// <summary>
/// Constants for file extensions used throughout the application.
/// </summary>
public static class FileExtensionConstants
{
    /// <summary>
    /// The refitter settings file extension.
    /// </summary>
    public const string Refitter = ".refitter";

    /// <summary>
    /// The generated C# file extension.
    /// </summary>
    public const string GeneratedCSharp = ".g.cs";

    /// <summary>
    /// The C# file extension.
    /// </summary>
    public const string CSharp = ".cs";
}

/// <summary>
/// Constants for content types and HTTP headers.
/// </summary>
public static class ContentTypeConstants
{
    /// <summary>
    /// The application/json content type.
    /// </summary>
    public const string Json = "application/json";

    /// <summary>
    /// The application/xml content type.
    /// </summary>
    public const string Xml = "application/xml";
}

/// <summary>
/// Constants for package and library names.
/// </summary>
public static class PackageConstants
{
    /// <summary>
    /// The Polly package name.
    /// </summary>
    public const string Polly = "Polly";

    /// <summary>
    /// The Akavache package name.
    /// </summary>
    public const string Akavache = "Akavache";

    /// <summary>
    /// The MonkeyCache package name.
    /// </summary>
    public const string MonkeyCache = "MonkeyCache";

    /// <summary>
    /// The AutoMapper package name.
    /// </summary>
    public const string AutoMapper = "AutoMapper";

    /// <summary>
    /// The Mapster package name.
    /// </summary>
    public const string Mapster = "Mapster";

    /// <summary>
    /// The MediatR package name.
    /// </summary>
    public const string MediatR = "MediatR";
}

/// <summary>
/// Constants for .NET type names used in code generation.
/// </summary>
public static class DotNetTypeConstants
{
    /// <summary>
    /// System.DateTimeOffset type name.
    /// </summary>
    public const string DateTimeOffset = "System.DateTimeOffset";

    /// <summary>
    /// System.TimeSpan type name.
    /// </summary>
    public const string TimeSpan = "System.TimeSpan";

    /// <summary>
    /// System.Collections.Generic.Dictionary type name.
    /// </summary>
    public const string Dictionary = "System.Collections.Generic.Dictionary";
}

/// <summary>
/// Constants for default file names.
/// </summary>
public static class FilenameConstants
{
    /// <summary>
    /// Default output filename.
    /// </summary>
    public const string DefaultOutput = "Output.cs";

    /// <summary>
    /// Default refitter settings filename.
    /// </summary>
    public const string DefaultSettingsFile = ".refitter";
}
