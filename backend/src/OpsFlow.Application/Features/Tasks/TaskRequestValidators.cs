using FluentValidation;

namespace OpsFlow.Application.Features.Tasks;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(10000);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(10000);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public sealed class MoveTaskRequestValidator : AbstractValidator<MoveTaskRequest>
{
    public MoveTaskRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddTaskCommentRequestValidator : AbstractValidator<AddTaskCommentRequest>
{
    public AddTaskCommentRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
    }
}
