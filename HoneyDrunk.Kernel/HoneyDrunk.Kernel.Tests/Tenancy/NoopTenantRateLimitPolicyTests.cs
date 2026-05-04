using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Tenancy;
using HoneyDrunk.Kernel.Tenancy;

namespace HoneyDrunk.Kernel.Tests.Tenancy;

public sealed class NoopTenantRateLimitPolicyTests
{
    [Fact]
    public async Task EvaluateAsync_ForInternalTenant_AllowsWithoutRetryOrReason()
    {
        // Arrange
        var policy = new NoopTenantRateLimitPolicy();

        // Act
        var decision = await policy.EvaluateAsync(TenantId.Internal, "notify.email", CancellationToken.None);

        // Assert
        decision.Outcome.Should().Be(TenantRateLimitOutcome.Allow);
        decision.RetryAfter.Should().BeNull();
        decision.Reason.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_ForExternalTenant_AllowsByDefault()
    {
        // Arrange
        var policy = new NoopTenantRateLimitPolicy();

        // Act
        var decision = await policy.EvaluateAsync(TenantId.NewId(), "notify.email", CancellationToken.None);

        // Assert
        decision.Outcome.Should().Be(TenantRateLimitOutcome.Allow);
    }
}
