using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Identity;

public class NodeIdTests
{
    [Theory]
    [InlineData("test-node")]
    [InlineData("payment-node")]
    [InlineData("auth-gateway")]
    [InlineData("abc")]
    [InlineData("a-b-c")]
    [InlineData("node123")]
    [InlineData("123node")]
    public void Constructor_ValidNodeId_CreatesNodeId(string value)
    {
        // Act
        var nodeId = new NodeId(value);

        // Assert
        nodeId.Value.Should().Be(value);
        nodeId.ToString().Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrWhitespace_ThrowsArgumentException(string? value)
    {
        // Act
        var act = () => new NodeId(value!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("ab")] // Too short
    [InlineData("a")] // Too short
    public void Constructor_TooShort_ThrowsArgumentException(string value)
    {
        // Act
        var act = () => new NodeId(value);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*between 3 and 64 characters*");
    }

    [Fact]
    public void Constructor_TooLong_ThrowsArgumentException()
    {
        // Arrange
        var value = new string('a', 65);

        // Act
        var act = () => new NodeId(value);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*between 3 and 64 characters*");
    }

    [Theory]
    [InlineData("Test-Node")] // Uppercase
    [InlineData("test_node")] // Underscore
    [InlineData("test node")] // Space
    [InlineData("test.node")] // Dot
    [InlineData("-test")] // Starts with hyphen
    [InlineData("test-")] // Ends with hyphen
    [InlineData("test--node")] // Double hyphen
    public void Constructor_InvalidFormat_ThrowsArgumentException(string value)
    {
        // Act
        var act = () => new NodeId(value);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*kebab-case*");
    }

    [Theory]
    [InlineData("test-node", true)]
    [InlineData("payment123", true)]
    [InlineData("abc", true)]
    [InlineData("Test-Node", false)]
    [InlineData("test_node", false)]
    [InlineData("ab", false)]
    [InlineData("", false)]
    public void IsValid_VariousInputs_ReturnsExpectedResult(string value, bool expected)
    {
        // Act
        var result = NodeId.IsValid(value, out var errorMessage);

        // Assert
        result.Should().Be(expected);
        if (expected)
        {
            errorMessage.Should().BeNull();
        }
        else
        {
            errorMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Theory]
    [InlineData("test-node", true)]
    [InlineData("invalid_format", false)]
    [InlineData("ab", false)]
    public void TryParse_VariousInputs_ReturnsExpectedResult(string value, bool expectedSuccess)
    {
        // Act
        var result = NodeId.TryParse(value, out var nodeId);

        // Assert
        result.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            nodeId.Value.Should().Be(value);
        }
        else
        {
            nodeId.Value.Should().BeEmpty();
        }
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        // Arrange
        var nodeId = new NodeId("test-node");

        // Act
        string value = nodeId;

        // Assert
        value.Should().Be("test-node");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        // Arrange
        var nodeId1 = new NodeId("test-node");
        var nodeId2 = new NodeId("test-node");

        // Act & Assert
        nodeId1.Should().Be(nodeId2);
        (nodeId1 == nodeId2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        // Arrange
        var nodeId1 = new NodeId("test-node");
        var nodeId2 = new NodeId("other-node");

        // Act & Assert
        nodeId1.Should().NotBe(nodeId2);
        (nodeId1 != nodeId2).Should().BeTrue();
    }
}
