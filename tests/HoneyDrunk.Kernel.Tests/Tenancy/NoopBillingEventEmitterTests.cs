using AwesomeAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Tenancy;
using HoneyDrunk.Kernel.Tenancy;

namespace HoneyDrunk.Kernel.Tests.Tenancy;

public sealed class NoopBillingEventEmitterTests
{
    [Fact]
    public async Task EmitAsync_CompletesForBillingEvent()
    {
        // Arrange
        var emitter = new NoopBillingEventEmitter();
        var billingEvent = new BillingEvent(
            TenantId.Internal,
            "notify.delivery.success",
            "email",
            Units: 1,
            DateTimeOffset.UtcNow,
            "corr-123",
            new Dictionary<string, string>());

        // Act
        var act = async () => await emitter.EmitAsync(billingEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
