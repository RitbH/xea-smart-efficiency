using Xpo.Smart.Efficiency.Shared.Extensions;
using Xpo.Smart.Efficiency.Shared.Extensions.Constants;

namespace Xpo.Smart.Efficiency.Shared.Models
{
    public class JobType
    {
        public static readonly JobType Driver = new JobType { Code = "DRIVER", Title = "Driver" };
        public static readonly JobType DockWorker = new JobType { Code = "DOCKWORKER", Title = "Dock Worker" };

        public string Code { get; set; }

        public string Title { get; set; }

        public static string GetJobTitleByJobCode(string jobCode)
        {
            return jobCode.IgnoreCaseEquals(Driver.Code) ? Driver.Title : DockWorker.Title ?? Defaults.Placeholder;
        }
    }
}
