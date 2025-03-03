using DataAccess;
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
 
public class CreateCampaignHandler(IJsonFileRepository repository, IMediator mediator) : IRequestHandler<CreateCampaign, Result<CreateCampaignResponse>>
{
    public async Task<Result<CreateCampaignResponse>> Handle(CreateCampaign createCampaign, CancellationToken cancellationToken)
    {
        var request = createCampaign.Request;
        var createdTimestamp = DateTimeOffset.UtcNow;
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Organizer = request.Organizer,
            Status = CampaignStatus.Inactive, // Campaigns are inactive by default, must be activated explicitly
            Dates = [.. request.Dates?.Select(date => new CampaignDate
            {
                Date = date.Date,
                StartTime = date.StartTime,
                EndTime = date.EndTime,
                Status = CampaignDateStatus.Active, // Dates are active by default, must be hidden explicitly
                DepartmentAssignments = [.. date.DepartmentAssignments?.Select(assignment => new DepartmentAssignment
                {
                    DepartmentName = assignment.DepartmentName,
                    NumberOfSeats = assignment.NumberOfSeats,
                    ReservedRatioForGirls = assignment.ReservedRatioForGirls
                }) ?? []],
            }) ?? []],
            ReservedRatioForGirls = request.ReservedRatioForGirls,
            PurgeDate = request.PurgeDate,
            CreatedAt = createdTimestamp,
            UpdatedAt = createdTimestamp,
        };
 
        await repository.Create(campaign.IdString, campaign);
        await mediator.Publish(new CampaignChangedNotification(campaign.Id), cancellationToken);
 
        return Result.Ok(new CreateCampaignResponse(campaign.Id));
    }
}