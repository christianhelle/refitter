﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// Provide settings for Refit generator.
/// </summary>
[ExcludeFromCodeCoverage]
public class RefitGeneratorSettings
{
    /// <summary>
    /// Gets or sets the path to the Open API.
    /// </summary>
    public string OpenApiPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the namespace for the generated code. (default: GeneratedCode)
    /// </summary>
    public string Namespace { get; set; } = "GeneratedCode";

    /// <summary>
    /// Gets or sets the naming settings.
    /// </summary>
    public NamingSettings Naming { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether contracts should be generated.
    /// </summary>
    public bool GenerateContracts { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether XML doc comments should be generated.
    /// </summary>
    public bool GenerateXmlDocCodeComments { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to add auto-generated header.
    /// </summary>
    public bool AddAutoGeneratedHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to return <c>IApiResponse</c> objects.
    /// </summary>
    public bool ReturnIApiResponse { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to generate operation headers.
    /// </summary>
    public bool GenerateOperationHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets the generated type accessibility. (default: Public)
    /// </summary>
    public TypeAccessibility TypeAccessibility { get; set; } = TypeAccessibility.Public;

    /// <summary>
    /// Enable or disable the use of cancellation tokens.
    /// </summary>
    public bool UseCancellationTokens { get; set; }

    /// <summary>
    /// Set to <c>true</c> to explicitly format date query string parameters
    /// in ISO 8601 standard date format using delimiters (for example: 2023-06-15)
    /// </summary>
    public bool UseIsoDateFormat { get; set; }

    public string[] AdditionalNamespaces { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Configurable settings for naming in the client API
/// </summary>
[ExcludeFromCodeCoverage]
public class NamingSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the OpenApi title should be used. Default is true.
    /// </summary>
    public bool UseOpenApiTitle { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the Interface. Default is "ApiClient".
    /// </summary>
    public string InterfaceName { get; set; } = "ApiClient";
}

/// <summary>
/// Specifies the accessibility of a type.
/// </summary>
public enum TypeAccessibility
{
    /// <summary>
    /// Indicates that the type is accessible by any assembly that references it.
    /// </summary>
    Public,

    /// <summary>
    /// Indicates that the type is only accessible within its own assembly.
    /// </summary>
    Internal
}