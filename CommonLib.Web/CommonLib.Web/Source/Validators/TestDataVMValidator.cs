using CommonLib.Web.Source.ViewModels;
using FluentValidation;

namespace CommonLib.Web.Source.Validators
{
    public class TestDataVMValidator : AbstractValidator<TestDataVM>
    {
        public TestDataVMValidator()
        {
            RuleFor(item => item.Name).NotEmpty().MaximumLength(50);
        }
    }
}
