using AwesomeAssertions;
using HoneyDrunk.Kernel.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class WellKnownNodesTests
{
    private static readonly KeyValuePair<string, NodeId>[] Nodes =
    [
        new("honeydrunk-kernel", WellKnownNodes.Core.Kernel),
        new("honeydrunk-transport", WellKnownNodes.Core.Transport),
        new("honeydrunk-vault", WellKnownNodes.Core.Vault),
        new("honeydrunk-vault-rotation", WellKnownNodes.Core.VaultRotation),
        new("honeydrunk-auth", WellKnownNodes.Core.Auth),
        new("honeydrunk-web-rest", WellKnownNodes.Core.WebRest),
        new("honeydrunk-data", WellKnownNodes.Core.Data),
        new("honeydrunk-audit", WellKnownNodes.Core.Audit),
        new("honeydrunk-pulse", WellKnownNodes.Ops.Pulse),
        new("honeydrunk-communications", WellKnownNodes.Ops.Communications),
        new("honeydrunk-notify", WellKnownNodes.Ops.Notify),
        new("honeydrunk-actions", WellKnownNodes.Ops.Actions),
        new("honeydrunk-architecture", WellKnownNodes.Meta.Architecture),
        new("honeydrunk-studios", WellKnownNodes.Meta.Studios),
        new("honeydrunk-lore", WellKnownNodes.Meta.Lore),
        new("honeydrunk-ai", WellKnownNodes.AI.Ai),
        new("honeydrunk-capabilities", WellKnownNodes.AI.Capabilities),
        new("honeydrunk-agents", WellKnownNodes.AI.Agents),
        new("honeydrunk-memory", WellKnownNodes.AI.Memory),
        new("honeydrunk-knowledge", WellKnownNodes.AI.Knowledge),
        new("honeydrunk-flow", WellKnownNodes.AI.Flow),
        new("honeydrunk-operator", WellKnownNodes.AI.Operator),
        new("honeydrunk-evals", WellKnownNodes.AI.Evals),
        new("honeydrunk-sim", WellKnownNodes.AI.Sim),
    ];

    public static TheoryData<string, NodeId> CanonicalNodeIds => Nodes.Aggregate(
        new TheoryData<string, NodeId>(),
        (data, node) =>
        {
            data.Add(node.Key, node.Value);
            return data;
        });

    [Theory]
    [MemberData(nameof(CanonicalNodeIds))]
    public void WellKnownNodeIds_MatchCanonicalGridIds(string expected, NodeId actual)
    {
        actual.Value.Should().Be(expected);
        actual.ToString().Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(CanonicalNodeIds))]
    public void WellKnownNodeIds_AreValidKebabCaseNodeIds(string expected, NodeId actual)
    {
        actual.Value.Should().Be(expected);
        NodeId.IsValid(actual.Value, out var errorMessage).Should().BeTrue(errorMessage);
    }

    [Fact]
    public void WellKnownNodeIds_AreUnique()
    {
        Nodes.Select(node => node.Key).Should().OnlyHaveUniqueItems();
    }
}
