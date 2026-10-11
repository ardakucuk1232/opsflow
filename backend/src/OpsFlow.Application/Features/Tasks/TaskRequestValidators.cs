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

public sealed class TaskListQueryValidator : AbstractValidator<TaskListQuery>
{
    public TaskListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleForEach(x => x.Status).IsInEnum();
        RuleForEach(x => x.Priority).IsInEnum();
        RuleFor(x => x.Sort).IsInEnum();
        RuleFor(x => x.Unassigned)
            .Equal(false)
            .When(x => x.AssigneeId is not null)
            .WithMessage("Choose either an assignee or unassigned tasks, not both.");
        RuleFor(x => x.DueTo)
            .GreaterThanOrEqualTo(x => x.DueFrom)
            .When(x => x.DueFrom is not null && x.DueTo is not null);
    }
}
