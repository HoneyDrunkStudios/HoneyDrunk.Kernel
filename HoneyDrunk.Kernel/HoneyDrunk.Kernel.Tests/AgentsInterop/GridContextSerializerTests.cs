using FluentAssertions;
using HoneyDrunk.Kernel.AgentsInterop;
using HoneyDrunk.Kernel.Context;

namespace HoneyDrunk.Kernel.Tests.AgentsInterop;

public class GridContextSerializerTests
{
    [Fact]
    public void Serialize_NullContext_ThrowsArgumentNullException()
    {
        var act = () => GridContextSerializer.Serialize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Serialize_ValidContext_ReturnsJsonString()
    {
        var context = new GridContext("corr-123", "test-node", "test-studio", "production");

        var json = GridContextSerializer.Serialize(context);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("correlationId");
        json.Should().Contain("nodeId");
        json.Should().Contain("studioId");
        json.Should().Contain("environment");
    }

    [Fact]
    public void Serialize_IncludesAllRequiredFields()
    {
        var context = new GridContext("corr-123", "test-node", "test-studio", "production");

        var json = GridContextSerializer.Serialize(context);

        json.Should().Contain("\"correlationId\":\"corr-123\"");
        json.Should().Contain("\"nodeId\":\"test-node\"");
        json.Should().Contain("\"studioId\":\"test-studio\"");
        json.Should().Contain("\"environment\":\"production\"");
    }

    [Fact]
    public void Serialize_WithCausationId_IncludesCausation()
    {
        var context = new GridContext(
            "corr-123",
            "test-node",
            "test-studio",
            "production",
            causationId: "cause-456");

        var json = GridContextSerializer.Serialize(context);

        json.Should().Contain("\"causationId\":\"cause-456\"");
    }

    [Fact]
    public void Serialize_WithoutCausationId_IncludesNullCausation()
    {
        var context = new GridContext("corr-123", "test-node", "test-studio", "production");

        var json = GridContextSerializer.Serialize(context);

        json.Should().Contain("\"causationId\":null");
    }

    [Fact]
    public void Serialize_DefaultBehavior_FiltersSensitiveBaggage()
    {
        var baggage = new Dictionary<string, string>
        {
            ["user-id"] = "user-123",
            ["api-key"] = "secret-key",
            ["password"] = "secret-pass",
            ["safe-value"] = "visible"
        };
        var context = new GridContext(
            "corr-123",
            "test-node",
            "test-studio",
            "production",
            baggage: baggage);

        var json = GridContextSerializer.Serialize(context, includeFullBaggage: false);

        json.Should().Contain("safe-value");
        json.Should().Contain("user-id");
        json.Should().NotContain("api-key");
        json.Should().NotContain("password");
    }

    [Fact]
    public void Serialize_IncludeFullBaggage_IncludesAllBaggage()
    {
        var baggage = new Dictionary<string, string>
        {
            ["user-id"] = "user-123",
            ["api-key"] = "secret-key",
            ["safe-value"] = "visible"
        };
        var context = new GridContext(
            "corr-123",
            "test-node",
            "test-studio",
            "production",
            baggage: baggage);

        var json = GridContextSerializer.Serialize(context, includeFullBaggage: true);

        json.Should().Contain("user-id");
        json.Should().Contain("api-key");
        json.Should().Contain("safe-value");
    }

    [Fact]
    public void Serialize_FiltersSensitiveKeyPatterns()
    {
        var baggage = new Dictionary<string, string>
        {
            ["database-password"] = "secret",
            ["api-token"] = "secret",
            ["encryption-key"] = "secret",
            ["user-credential"] = "secret",
            ["app-secret"] = "secret",
            ["safe-data"] = "visible"
        };
        var context = new GridContext(
            "corr-123",
            "test-node",
            "test-studio",
            "production",
            baggage: baggage);

        var json = GridContextSerializer.Serialize(context, includeFullBaggage: false);

        json.Should().Contain("safe-data");
        json.Should().NotContain("database-password");
        json.Should().NotContain("api-token");
        json.Should().NotContain("encryption-key");
        json.Should().NotContain("user-credential");
        json.Should().NotContain("app-secret");
    }

    [Fact]
    public void Serialize_UsesCamelCase()
    {
        var context = new GridContext("corr-123", "test-node", "test-studio", "production");

        var json = GridContextSerializer.Serialize(context);

        json.Should().Contain("correlationId");
        json.Should().NotContain("CorrelationId");
        json.Should().Contain("createdAtUtc");
        json.Should().NotContain("CreatedAtUtc");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Deserialize_NullOrWhitespaceJson_ThrowsArgumentException(string? json)
    {
        var act = () => GridContextSerializer.Deserialize(json!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deserialize_ValidJson_ReturnsGridContext()
    {
        var json = @"{
            ""correlationId"": ""corr-123"",
            ""nodeId"": ""test-node"",
            ""studioId"": ""test-studio"",
            ""environment"": ""production""
        }";

        var context = GridContextSerializer.Deserialize(json);

        context.Should().NotBeNull();
        context!.CorrelationId.Should().Be("corr-123");
        context.NodeId.Should().Be("test-node");
        context.StudioId.Should().Be("test-studio");
        context.Environment.Should().Be("production");
    }

    [Fact]
    public void Deserialize_WithCausationId_DeserializesCausation()
    {
        var json = @"{
            ""correlationId"": ""corr-123"",
            ""causationId"": ""cause-456"",
            ""nodeId"": ""test-node"",
            ""studioId"": ""test-studio"",
            ""environment"": ""production""
        }";

        var context = GridContextSerializer.Deserialize(json);

        context.Should().NotBeNull();
        context!.CausationId.Should().Be("cause-456");
    }

    [Fact]
    public void Deserialize_WithBaggage_DeserializesBaggage()
    {
        var json = @"{
            ""correlationId"": ""corr-123"",
            ""nodeId"": ""test-node"",
            ""studioId"": ""test-studio"",
            ""environment"": ""production"",
            ""baggage"": {
                ""key1"": ""value1"",
                ""key2"": ""value2""
            }
        }";

        var context = GridContextSerializer.Deserialize(json);

        context.Should().NotBeNull();
        context!.Baggage.Should().HaveCount(2);
        context.Baggage["key1"].Should().Be("value1");
        context.Baggage["key2"].Should().Be("value2");
    }

    [Fact]
    public void Deserialize_WithCreatedAtUtc_DeserializesTimestamp()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var json = $@"{{
            ""correlationId"": ""corr-123"",
            ""nodeId"": ""test-node"",
            ""studioId"": ""test-studio"",
            ""environment"": ""production"",
            ""createdAtUtc"": ""{timestamp:O}""
        }}";

        var context = GridContextSerializer.Deserialize(json);

        context.Should().NotBeNull();
        context!.CreatedAtUtc.Should().BeCloseTo(timestamp, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deserialize_InvalidJson_ReturnsNull()
    {
        var json = "{ invalid json }";

        var context = GridContextSerializer.Deserialize(json);

        context.Should().BeNull();
    }

    [Fact]
    public void Deserialize_MissingRequiredField_ReturnsNull()
    {
        var json = @"{
            ""correlationId"": ""corr-123"",
            ""nodeId"": ""test-node""
        }";

        var context = GridContextSerializer.Deserialize(json);

        context.Should().BeNull();
    }

    [Fact]
    public void Deserialize_EmptyRequiredField_ReturnsNull()
    {
        var json = @"{
            ""correlationId"": """",
            ""nodeId"": ""test-node"",
            ""studioId"": ""test-studio"",
            ""environment"": ""production""
        }";

        var context = GridContextSerializer.Deserialize(json);

        context.Should().BeNull();
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        var original = new GridContext(
            "corr-123",
            "test-node",
            "test-studio",
            "production",
            causationId: "cause-456",
            baggage: new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            });

        var json = GridContextSerializer.Serialize(original, includeFullBaggage: true);
        var deserialized = GridContextSerializer.Deserialize(json);

        deserialized.Should().NotBeNull();
        deserialized!.CorrelationId.Should().Be(original.CorrelationId);
        deserialized.NodeId.Should().Be(original.NodeId);
        deserialized.StudioId.Should().Be(original.StudioId);
        deserialized.Environment.Should().Be(original.Environment);
        deserialized.CausationId.Should().Be(original.CausationId);
        deserialized.Baggage.Should().BeEquivalentTo(original.Baggage);
    }

    [Fact]
    public void Serialize_EmptyBaggage_SerializesEmptyObject()
    {
        var context = new GridContext("corr-123", "test-node", "test-studio", "production");

        var json = GridContextSerializer.Serialize(context);

        json.Should().Contain("\"baggage\":{}");
    }

    [Fact]
    public void Deserialize_NullBaggageValues_SkipsNullValues()
    {
        var json = @"{
            ""correlationId"": ""corr-123"",
            ""nodeId"": ""test-node"",
            ""studioId"": ""test-studio"",
            ""environment"": ""production"",
            ""baggage"": {
                ""key1"": ""value1"",
                ""key2"": null
            }
        }";

        var context = GridContextSerializer.Deserialize(json);

        context.Should().NotBeNull();
        context!.Baggage.Should().HaveCount(1);
        context.Baggage.Should().ContainKey("key1");
        context.Baggage.Should().NotContainKey("key2");
    }
}
