using FluentValidation;

namespace OpsFlow.Application.Features.Projects;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public const string KeyPattern = "^[A-Za-z][A-Za-z0-9]{1,9}$";

    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Key)
            .NotEmpty()
            .Matches(KeyPattern)
            .WithMessage("Key must be 2 to 10 letters or digits and start with a letter.");

        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Status).IsInEnum();

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate is not null && x.EndDate is not null)
            .WithMessage("End date cannot be before the start date.");
    }
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Status).IsInEnum();

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate is not null && x.EndDate is not null)
            .WithMessage("End date cannot be before the start date.");
    }
}

public sealed class AddProjectMemberRequestValidator : AbstractValidator<AddProjectMemberRequest>
{
    public AddProjectMemberRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class UpdateProjectMemberRequestValidator : AbstractValidator<UpdateProjectMemberRequest>
{
    public UpdateProjectMemberRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
