using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Abstractions;

/// <summary>
/// Well-known Environment identifiers for deployment stages.
/// </summary>
/// <remarks>
/// Environments represent deployment stages and isolation boundaries.
/// Use these static values for consistency. Prefer this registry over EnvironmentId.WellKnown.
/// </remarks>
public static class Environments
{
    /// <summary>Production environment - live customer traffic.</summary>
    public static readonly EnvironmentId Production = EnvironmentId.WellKnown.Production;

    /// <summary>Staging environment - pre-production validation.</summary>
    public static readonly EnvironmentId Staging = EnvironmentId.WellKnown.Staging;

    /// <summary>Development environment - active feature development.</summary>
    public static readonly EnvironmentId Development = EnvironmentId.WellKnown.Development;

    /// <summary>Testing environment - automated test execution.</summary>
    public static readonly EnvironmentId Testing = EnvironmentId.WellKnown.Testing;

    /// <summary>Performance environment - load and stress testing.</summary>
    public static readonly EnvironmentId Performance = EnvironmentId.WellKnown.Performance;

    /// <summary>Integration environment - third-party integration testing.</summary>
    public static readonly EnvironmentId Integration = EnvironmentId.WellKnown.Integration;

    /// <summary>Local environment - developer workstation.</summary>
    public static readonly EnvironmentId Local = EnvironmentId.WellKnown.Local;
}
