using FluentValidation;
using System;
using WebApi.DTO;

namespace WebApi.Validators
{
    public class ReportDTOValidator : AbstractValidator<ReportDTO>
    {
        public ReportDTOValidator()
        {
            RuleFor(x => x.Year)
                .NotEmpty().WithMessage("Год обязателен")
                .Matches(@"^\d{4}$").WithMessage("Год должен быть ровно 4 цифрами (например, 2024)")
                .Custom((yearStr, context) =>
                {
                    if (int.TryParse(yearStr, out var year))
                    {
                        if (year < 2000 || year > DateTime.UtcNow.Year + 1)
                            context.AddFailure("Год должен быть в диапазоне от 2000 до текущего года + 1");
                    }
                });

            RuleFor(x => x.Month)
                .NotEmpty().WithMessage("Месяц обязателен")
                .Matches(@"^(0?[1-9]|1[0-2])$").WithMessage("Месяц должен быть от 01 до 12")
                .Custom((monthStr, context) =>
                {
                    if (int.TryParse(monthStr, out var month))
                    {
                        // Дополнительная бизнес-проверка: нельзя запрашивать месяц в будущем (опционально)
                        var currentYear = DateTime.UtcNow.Year;
                        var currentMonth = DateTime.UtcNow.Month;

                        int.TryParse(context.InstanceToValidate.Year, out var checkYear);

                        if (checkYear == currentYear && month > currentMonth)
                            context.AddFailure("Нельзя запрашивать отчёт за будущий месяц");
                    }
                });

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Страница должна быть не меньше 1")
                .LessThanOrEqualTo(9999).WithMessage("Номер страницы слишком большой");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Размер страницы должен быть от 1 до 100 записей");
        }
    }
}
