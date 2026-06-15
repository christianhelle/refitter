using Refitter.Core.Settings;

namespace Refitter.Core;

internal class FileTransferAdapter : IApizrOptionsAdapter
{
    public bool CanApply(RefitGeneratorSettings settings)
    {
        return settings.ApizrSettings!.WithFileTransfer;
    }

    public void Apply(IApizrOptionsBuilder builder, RefitGeneratorSettings settings)
    {
        var isDependencyInjectionExtension = settings.DependencyInjectionSettings != null;

        if (isDependencyInjectionExtension)
        {
            if (settings.ApizrSettings!.WithMediation)
            {
                builder.AddPackage(ApizrPackages.Apizr_Integrations_FileTransfer_MediatR);
                builder.AppendOptionsCode("\n                .WithFileTransferMediation()");
            }
            else
            {
                builder.AddPackage(ApizrPackages.Apizr_Extensions_Microsoft_FileTransfer);
            }
        }
        else
        {
            builder.AddPackage(ApizrPackages.Apizr_Integrations_FileTransfer);
        }
    }
}
