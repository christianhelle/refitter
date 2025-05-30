using System.ComponentModel;

namespace Refitter.Core.Settings
{
    /// <summary>
    /// Enumeration of available Apizr packages.
    /// </summary>
    public enum ApizrPackages
    {
        /// <summary>
        /// Base Apizr package.
        /// </summary>
        [Description("dotnet add package Apizr")]
        Apizr = 1,

        /// <summary>
        /// Apizr Microsoft dependency injection extensions.
        /// </summary>
        [Description("dotnet add package Apizr.Extensions.Microsoft.DependencyInjection")]
        Apizr_Extensions_Microsoft_DependencyInjection = 2 | Apizr,

        /// <summary>
        /// Apizr Akavache integration.
        /// </summary>
        [Description("dotnet add package Apizr.Integrations.Akavache")]
        Apizr_Integrations_Akavache = 4 | Apizr,

        /// <summary>
        /// Apizr MonkeyCache integration.
        /// </summary>
        [Description("dotnet add package Apizr.Integrations.MonkeyCache, then write somewhere: Barrel.ApplicationId = \"YOUR_APPLICATION_NAME\";")]
        Apizr_Integrations_MonkeyCache = 8 | Apizr,

        /// <summary>
        /// Apizr Fusillade integration.
        /// </summary>
        [Description("dotnet add package Apizr.Integrations.Fusillade")]
        Apizr_Integrations_Fusillade = 16 | Apizr,

        /// <summary>
        /// Apizr AutoMapper integration.
        /// </summary>
        [Description("dotnet add package Apizr.Integrations.AutoMapper, then register AutoMapper")]
        Apizr_Integrations_AutoMapper = 32 | Apizr,

        /// <summary>
        /// Apizr Mapster integration.
        /// </summary>
        [Description("dotnet add package Apizr.Integrations.Mapster, then register Mapster")]
        Apizr_Integrations_Mapster = 64 | Apizr,

        /// <summary>
        /// Apizr file transfer integration.
        /// </summary>
        [Description("dotnet add package Apizr.Integrations.FileTransfer")]
        Apizr_Integrations_FileTransfer = 128 | Apizr,

        /// <summary>
        /// Apizr Microsoft caching extensions.
        /// </summary>
        [Description("dotnet add package Apizr.Extensions.Microsoft.Caching, then register your caching provider")]
        Apizr_Extensions_Microsoft_Caching = 256 | Apizr_Extensions_Microsoft_DependencyInjection,

        /// <summary>
        /// Apizr Microsoft file transfer extensions.
        /// </summary>
        [Description("dotnet add package Apizr.Extensions.Microsoft.FileTransfer")]
        Apizr_Extensions_Microsoft_FileTransfer = 512 | Apizr_Integrations_FileTransfer | Apizr_Extensions_Microsoft_DependencyInjection,

        /// <summary>
        /// Apizr MediatR integration.
        /// </summary>
        [Description("dotnet add package Apizr.Integrations.MediatR, then register MediatR")]
        Apizr_Integrations_MediatR = 1024 | Apizr_Extensions_Microsoft_DependencyInjection,

        /// <summary>
        /// Apizr file transfer MediatR integration.
        /// </summary>
        [Description("dotnet add package Apizr.Integrations.FileTransfer.MediatR, then register MediatR")]
        Apizr_Integrations_FileTransfer_MediatR = 2048 | Apizr_Integrations_MediatR | Apizr_Extensions_Microsoft_FileTransfer
    }
}
