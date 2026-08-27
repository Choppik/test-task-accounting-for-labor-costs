using FluentValidation;
using WebApi.Commands;

namespace WebApi.Validators
{
    public class UpdateTimeEntryValidator : AbstractValidator<UpdateTimeEntryCommand>
    {
        public UpdateTimeEntryValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("EmployeeId обязателен");
            RuleFor(x => x.ProjectId).NotEmpty().WithMessage("ProjectId обязателен");
            RuleFor(x => x.Date).NotEmpty().WithMessage("Дата обязательна");
            RuleFor(x => x.Hours)
                .GreaterThan(0).WithMessage("Часы должны быть > 0")
                .LessThanOrEqualTo(24).WithMessage("Часы не могут быть больше 24");
            RuleFor(x => x.ModifiedBy).NotEmpty().When(x => !string.IsNullOrEmpty(x.ModifiedBy)).WithMessage("ModifiedBy не может быть пустым, если указан");
        }
    }
}
