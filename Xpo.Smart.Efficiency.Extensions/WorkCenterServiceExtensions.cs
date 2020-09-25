using System.Linq;
using Xpo.Smart.Core.Models;
using Xpo.Smart.Core.Services;

namespace Xpo.Smart.Efficiency.Shared.Extensions
{
    public static class WorkCenterServiceExtensions
    {
        public static Workcenter[] ListIncludeInvalids(this IWorkcenterService workCenterService, string siteCode)
        {
            return workCenterService.List(siteCode).Concat(workCenterService.ListInvalid(siteCode)).ToArray();
        }
    }
}
