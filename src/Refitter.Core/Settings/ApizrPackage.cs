using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Refitter.Core.Settings
{
    [Flags]
    public enum ApizrPackage
    {
        [Description("Install-Package Apizr")]
        Apizr = 1,

        [Description("Install-Package Apizr.Extensions.Microsoft.DependencyInjection")]
        Apizr_Extensions_Microsoft_DependencyInjection = 2 | Apizr,

        [Description("Install-Package Apizr.Integrations.Akavache")]
        Apizr_Integrations_Akavache = 4 | Apizr,

        [Description("Install-Package Apizr.Integrations.MonkeyCache, then write somewhere: Barrel.ApplicationId = \"YOUR_APPLICATION_NAME\";")]
        Apizr_Integrations_MonkeyCache = 8 | Apizr,

        [Description("Install-Package Apizr.Integrations.Fusillade")]
        Apizr_Integrations_Fusillade = 16 | Apizr,

        [Description("Install-Package Apizr.Integrations.AutoMapper, then register AutoMapper")]
        Apizr_Integrations_AutoMapper = 32 | Apizr,

        [Description("Install-Package Apizr.Integrations.Mapster, then register Mapster")]
        Apizr_Integrations_Mapster = 64 | Apizr,

        [Description("Install-Package Apizr.Integrations.FileTransfer")]
        Apizr_Integrations_FileTransfer = 128 | Apizr,

        [Description("Install-Package Apizr.Extensions.Microsoft.Caching, then register your caching provider")]
        Apizr_Extensions_Microsoft_Caching = 256 | Apizr_Extensions_Microsoft_DependencyInjection,

        [Description("Install-Package Apizr.Extensions.Microsoft.FileTransfer")]
        Apizr_Extensions_Microsoft_FileTransfer = 512 | Apizr_Integrations_FileTransfer | Apizr_Extensions_Microsoft_DependencyInjection,

        [Description("Install-Package Apizr.Integrations.MediatR, then register MediatR")]
        Apizr_Integrations_MediatR = 1024 | Apizr_Extensions_Microsoft_DependencyInjection,

        [Description("Install-Package Apizr.Integrations.FileTransfer.MediatR, then register MediatR")]
        Apizr_Integrations_FileTransfer_MediatR = 2048 | Apizr_Integrations_MediatR | Apizr_Extensions_Microsoft_FileTransfer,

        [Description("Install-Package Apizr.Integrations.Optional, then register MediatR")]
        Apizr_Integrations_Optional = 4096 | Apizr_Integrations_MediatR,

        [Description("Install-Package Apizr.Integrations.FileTransfer.Optional, then register MediatR")]
        Apizr_Integrations_FileTransfer_Optional = 8192 | Apizr_Integrations_Optional | Apizr_Integrations_FileTransfer_MediatR,
    }
}
