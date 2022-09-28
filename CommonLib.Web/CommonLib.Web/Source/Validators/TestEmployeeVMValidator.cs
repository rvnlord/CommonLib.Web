using System.Threading.Tasks;
using CommonLib.Source.Common.Utils.UtilClasses;
using CommonLib.Web.Source.ViewModels;
using FluentValidation;

namespace CommonLib.Web.Source.Validators
{
    public class TestEmployeeVMValidator : AbstractValidator<TestEmployeeVM>
    {
        public TestEmployeeVMValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;
            var e = new TestEmployeeVM();

            var name = e.GetPropertyDisplayName(() => e.Name);
            const int nameMin = 3;
            RuleFor(m => m.Name)
                .NotEmpty().WithMessage($"{name} can't be empty")
                .MinimumLength(nameMin).WithMessage($"{name} must contain at least {nameMin} characters");

            //var email = typeof(Employee).GetPropertyDisplayName(nameof(Employee.Email));
            var email = e.GetPropertyDisplayName(() => e.Email);
            const int emailMax = 255;
            RuleFor(m => m.Email)
                .NotEmpty().WithMessage($"{email} can't be empty")
                .Matches(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$").WithMessage($"Invalid {email} format")
                .MaximumLength(255).WithMessage($"{name} must can't contain more than {emailMax} characters");

            //var department = typeof(Employee).GetPropertyDisplayName(nameof(Employee.Department));
            var department = e.GetPropertyDisplayName(() => e.Department);
            RuleFor(m => m.Department)
                //.MustAsync(async (dept, _) =>
                //{
                //    await Task.Delay(4000, _).ConfigureAwait(false);
                //    return true;
                //})
                //.Must(dept =>
                //{
                //    Thread.Sleep(3000);
                //    return true;
                //})
                .NotEmpty().WithMessage($"{department} can't be empty");

            var domain = e.GetPropertyDisplayName(() => e.Domain);
            RuleFor(m => m.Domain).NotEmpty().WithMessage($"{domain} can't be empty");
        }
    }

}
