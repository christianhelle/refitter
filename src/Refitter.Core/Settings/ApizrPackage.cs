using System;
using System.Collections.Generic;
using System.Text;

namespace Refitter.Core.Settings
{
    [Flags]
    public enum ApizrPackage
    {
        Apizr = 1,
        Apizr_Extensions_Microsoft_DependencyInjection = 2 | Apizr,
        Apizr_Integrations_Akavache = 4 | Apizr,
        Apizr_Integrations_MonkeyCache = 8 | Apizr,
        Apizr_Integrations_Fusillade = 16 | Apizr,
        Apizr_Integrations_AutoMapper = 32 | Apizr,
        Apizr_Integrations_Mapster = 64 | Apizr,
        Apizr_Integrations_FileTransfer = 128 | Apizr,
        Apizr_Extensions_Microsoft_Caching = 256 | Apizr_Extensions_Microsoft_DependencyInjection,
        Apizr_Extensions_Microsoft_FileTransfer = 512 | Apizr_Integrations_FileTransfer | Apizr_Extensions_Microsoft_DependencyInjection,
        Apizr_Integrations_MediatR = 1024 | Apizr_Extensions_Microsoft_DependencyInjection,
        Apizr_Integrations_FileTransfer_MediatR = 2048 | Apizr_Integrations_MediatR | Apizr_Extensions_Microsoft_FileTransfer,
        Apizr_Integrations_Optional = 4096 | Apizr_Integrations_MediatR,
        Apizr_Integrations_FileTransfer_Optional = 8192 | Apizr_Integrations_Optional | Apizr_Integrations_FileTransfer_MediatR,
    }
}
