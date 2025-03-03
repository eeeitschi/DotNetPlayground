using System.Text.Json;
using System.Text.Json.Serialization;
using DataAccess;
using FluentValidation;
using MediatR;
using Registration;
using Registration.Api;
 
var builder = WebApplication.CreateBuilder(args);
 
builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateCampaignRequestValidator));
 
builder.AddJsonFileRepository();
builder.Services.AddMediatR();
builder.Services.AddExceptionHandler();
 
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions.Add("trace-id", ctx.HttpContext.TraceIdentifier);
    };
});

builder.Services.AddSingleton<ResultConverter>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
var app = builder.Build();
 
app.MapGet("/ping", () => "pong");
 
app.MapPost("/campaigns", async (CreateCampaignRequest request, IMediator mediator, ResultConverter converter) =>
{
    var result = await mediator.Send(new CreateCampaign(request));
    return converter.ToResult(result);
});
 
app.MapPatch("/campaigns/{campaignId}", async (Guid campaignId, UpdateCampaignRequest request, IMediator mediator, ResultConverter converter) =>
{
    var result = await mediator.Send(new UpdateCampaign(campaignId, request));
    return converter.ToResult(result);
});
 
app.MapPost("/campaigns/{campaignId}/activate", async (Guid campaignId, IMediator mediator, ResultConverter converter) =>
{
    var result = await mediator.Send(new ActivateCampaign(campaignId));
    return converter.ToResult(result);
});
 
app.MapGet("/campaigns/{campaignId}", async (Guid campaignId, IMediator mediator, ResultConverter converter) =>
{
    var result = await mediator.Send(new GetCampaign(campaignId));
    return converter.ToResult(result);
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