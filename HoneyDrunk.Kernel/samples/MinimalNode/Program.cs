using HoneyDrunk.Kernel.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Hosting;
using GridEnvironments = HoneyDrunk.Kernel.Abstractions.Environments;

var builder = WebApplication.CreateBuilder(args);

// Register HoneyDrunk Node with unified bootstrapper
builder.Services.AddHoneyDrunkNode(options =>
{
    // Identity from static registries (canonical pattern)
    options.NodeId = Nodes.Core.MinimalNode;
    options.SectorId = Sectors.Core;
    options.EnvironmentId = GridEnvironments.Development;
    
    // Version from configuration or assembly (avoid hardcoding in production)
    options.Version = builder.Configuration["Version"] 
        ?? typeof(Program).Assembly.GetName().Version?.ToString() 
        ?? "1.0.0";
    
    options.StudioId = builder.Configuration["Grid:StudioId"] ?? "demo-studio";
    
    // Metadata tags for observability
    options.Tags["region"] = "local";
    options.Tags["sample"] = "true";
});

var app = builder.Build();

// Validate all required services are registered (fail fast on misconfiguration)
app.Services.ValidateHoneyDrunkServices();

// Add Grid context middleware for HTTP request tracing
app.UseGridContext();

// Sample endpoint demonstrating context injection
app.MapGet("/", (INodeContext nodeContext, IGridContext gridContext) =>
{
    return Results.Ok(new
    {
        Message = "HoneyDrunk Minimal Node",
        Node = new
        {
            nodeContext.NodeId,
            nodeContext.Version,
            nodeContext.StudioId,
            nodeContext.Environment,
            nodeContext.LifecycleStage,
            nodeContext.StartedAtUtc
        },
        Request = new
        {
            gridContext.CorrelationId,
            gridContext.CausationId,
            gridContext.NodeId,
            gridContext.CreatedAtUtc
        }
    });
});

// Health endpoint (standard Grid endpoint)
app.MapGet("/health", () => Results.Ok(new 
{ 
    Status = "Healthy",
    Timestamp = DateTimeOffset.UtcNow 
}));

app.Run();
