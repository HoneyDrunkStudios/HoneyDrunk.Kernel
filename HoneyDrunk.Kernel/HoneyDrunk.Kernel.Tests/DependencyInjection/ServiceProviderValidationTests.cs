using AwesomeAssertions;
using HoneyDrunk.Kernel.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.Tests.DependencyInjection;

public class ServiceProviderValidationTests
{
    [Fact]
    public void Validate_EmptyServices_ThrowsListingAllRequiredServicesWithHint()
    {
        var validator = new ServiceProviderValidation();
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();

        var act = () => validator.Validate(provider);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Required HoneyDrunk services are not registered*")
            .Which.Message.Should().ContainAll(
                "INodeContext",
                "IGridContextAccessor",
                "IOperationContextAccessor",
                "IOperationContextFactory",
                "INodeDescriptor",
                "IErrorClassifier",
                "NodeLifecycleManager",
                "NodeLifecycleHost",
                "AddHoneyDrunkNode()");
    }
}
