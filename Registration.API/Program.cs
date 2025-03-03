using System.Text.Json;
using MediatR;
using Registration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
{
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.RegisterServicesFromAssemblyContaining<CreateCampaign>();
});

var app = builder.Build();

app.MapGet("/ping", () => "pong");

app.MapPost("/campaigns", async (CreateCampaignRequest request, IMediator sender) =>
{
    var result = await sender.Send(new CreateCampaign(request));
    if (result.IsFailed)
    {
        return Results.BadRequest();
    }
    return Results.Ok(result.Value);
});

app.MapGet("campaigns/changes", async (HttpContext ctx, IMediator mediator, CancellationToken cancellationToken) =>
{
    async void OnCampaignChanged(object? sender, CampaignChangedNotification ea)
    {
        var msg = JsonSerializer.Serialize(ea.CampaignId);
 
        await ctx.Response.WriteAsync($"data: ", cancellationToken);
        await ctx.Response.WriteAsync(msg, cancellationToken);
        await ctx.Response.WriteAsync("\n\n", cancellationToken);
        await ctx.Response.Body.FlushAsync(cancellationToken);
    }
 
    CampaignNotification.CampaignChanged += OnCampaignChanged;
    try
    {
        ctx.Response.Headers.Append("Content-Type", "text/event-stream");
        await ctx.Response.WriteAsync(": stream is starting\n\n", cancellationToken);
        await ctx.Response.Body.FlushAsync(cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }
    catch (TaskCanceledException)
    {
    }
    finally
    {
        CampaignNotification.CampaignChanged -= OnCampaignChanged;
    }
});

app.Run();