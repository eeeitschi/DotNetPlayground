using FluentResults;
using MediatR;

namespace Registration;

public record CreateCampaign(CreateCampaignRequest Request) : IRequest<Result<CreateCampaignResponse>>;

public record CreateCampaignRequest(
    string Name,
    string Organizer,
    CreateDateRequest[]? Dates = null,
    decimal? ReservedRatioForGirls = null,
    DateOnly? PurgeDate = null);

public record CreateDateRequest(
    DateOnly Date,
    DepartmentAssignmentRequest[]? DepartmentAssignments = null,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null
);

public record DepartmentAssignmentRequest(
    string DepartmentName,
    short NumberOfSeats,
    decimal? ReservedRatioForGirls = null
);

public record CreateCampaignResponse(
    Guid Id
    // Many APIs return more data than just the ID. This decision has to be made
    // on a case-by-case basis.
);

public class CreateCampaignHandler(IMediator mediator) : IRequestHandler<CreateCampaign, Result<CreateCampaignResponse>>
{
    public async Task<Result<CreateCampaignResponse>> Handle(CreateCampaign request, CancellationToken cancellationToken)
    {
        var campaignId = Guid.NewGuid();
        await mediator.Publish(new CampaignChangedNotification(campaignId));
        return Result.Ok(new CreateCampaignResponse(campaignId));
    }
}