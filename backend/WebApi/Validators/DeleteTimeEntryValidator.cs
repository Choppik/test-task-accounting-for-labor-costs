using FluentValidation;
using WebApi.Commands;

namespace WebApi.Validators
{
    public class DeleteTimeEntryValidator : AbstractValidator<DeleteTimeEntryCommand>
    {
        public DeleteTimeEntryValidator() 
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id записи обязателен");
        }
    }
}
