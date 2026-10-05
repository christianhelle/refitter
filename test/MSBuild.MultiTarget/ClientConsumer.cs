using Refitter.MSBuild.MultiTarget.Petstore;

namespace Refitter.MSBuild.MultiTarget;

// Fails to compile unless the generated interface is part of every inner build
public class ClientConsumer(ISwaggerPetstore client)
{
    public ISwaggerPetstore Client { get; } = client;
}
