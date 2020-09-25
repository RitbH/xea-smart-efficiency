using AutoMapper;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Common.Extensions;
using Xpo.Smart.Core.Context;
using Xpo.Smart.Core.DataCache;
using Xpo.Smart.Core.Extensions;
using Xpo.Smart.Core.Models;
using Xpo.Smart.Efficiency.Engine.Helpers;
using Xpo.Smart.Efficiency.Engine.Interfaces;
using Xpo.Smart.Efficiency.Shared.DataCache.Core;
using Xpo.Smart.Efficiency.Shared.DataCache.EfficiencyTimesheet;
using Xpo.Smart.Efficiency.Shared.DataCache.Transactions;
using Xpo.Smart.Efficiency.Shared.Extensions;
using Xpo.Smart.Efficiency.Shared.Models;
using Xpo.Smart.Efficiency.Shared.Services.CachedLookups;
using IEmployeeService = Xpo.Smart.Efficiency.Shared.Services.Employee.IEmployeeService;

namespace Xpo.Smart.Efficiency.Engine.Providers.Shifts
{
    public class EfficiencyShiftProvider : IEfficiencyShiftProvider
    {
        private readonly IEmployeeService _employeeService;        
        private readonly IDataCacheProvider<EfficiencyTimesheetCache> _timesheetCacheProvider;        
        private readonly ICacheRepositoryFactory<ITransactionCacheRepository> _transactionCacheRepositoryFactory;
        private readonly ISiteContextProvider _siteContextProvider;
        private readonly ICachedLookupService _cachedLookupService;
        
        public EfficiencyShiftProvider(
            IDataCacheProvider<EfficiencyTimesheetCache> timesheetCacheProvider,
            IEmployeeService employeeService, 
            ISiteContextProvider siteContextProvider, 
            ICacheRepositoryFactory<ITransactionCacheRepository> transactionCacheRepositoryFactory, 
            ICachedLookupService cachedLookupService)
        {
            _employeeService = employeeService;
            _timesheetCacheProvider = timesheetCacheProvider;
            _siteContextProvider = siteContextProvider;
            _transactionCacheRepositoryFactory = transactionCacheRepositoryFactory;
            _cachedLookupService = cachedLookupService;            
        }

        private readonly static Mapper _mapper;
        static EfficiencyShiftProvider()
        {
            var config = new MapperConfiguration(cfg => cfg.CreateMap<EfficiencyShift, EfficiencyShift>());
            _mapper = new Mapper(config);
        }

        public IEnumerable<EfficiencyShift> GetShiftsForSiteEmployeeCodes(string siteCode, DateTime siteNow, IEnumerable<string> siteEmployeeCodes, DateTime startTime, DateTime endTime)
        {            
            var employees = _employeeService.GetEmployees(siteCode);
            var siteEmployeeHash = new HashSet<string>(siteEmployeeCodes);
            var affectedEmployees = employees.Where(r => r.SiteEmployees.Any(x => siteEmployeeHash.Contains(x.SiteEmployeeCode)));

            return GetShifts(siteCode, siteNow, employees, siteEmployeeCodes, startTime, endTime);            
        }

        public IEnumerable<EfficiencyShift> GetShiftsForTnaEmployeeCodes(string siteCode, DateTime siteNow, IEnumerable<string> tnaEmployeeCodes, DateTime startTime, DateTime endTime)
        {               
            var employees = _employeeService.GetEmployees(siteCode);
            var siteEmployeeHash = new HashSet<string>(tnaEmployeeCodes);
            var affectedEmployees = employees.Where(r => siteEmployeeHash.Contains(r.TnaEmployeeCode));
            var siteEmployeeCodes = affectedEmployees
                .SelectMany(r => r.SiteEmployees.Select(x => x.SiteEmployeeCode))
                .Distinct();                

            return GetShifts(siteCode, siteNow, employees, siteEmployeeCodes, startTime, endTime);
        }

        /// <summary>
        /// Returns a list of shifts that overlap a given date range based on an employee's time sheet records.
        /// In cases where an employee has transactions that do not fall within a given time sheet, a shift for those
        /// transactions is created and they are bucketed under there.
        /// </summary>
        /// <param name="siteCode"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        private IEnumerable<EfficiencyShift> GetShifts(string siteCode,DateTime siteNow, Employee[] employees, IEnumerable<string> siteEmployeeCodes, DateTime startTime, DateTime endTime)
        {
            var tnaEmployeeCodes = new HashSet<string>(employees.Select(r => r.TnaEmployeeCode));

            var extendedTimeSheets = _timesheetCacheProvider.GetCurrent().GetAll(siteCode).ToArray();
            var timeSheets = extendedTimeSheets  
                .Where(t=>tnaEmployeeCodes.Contains(t.TnaEmployeeCode))
                .Where(t => t.OperationalDate >= startTime.Date && t.OperationalDate <= endTime.Date);

            var siteContext = _siteContextProvider.GetSiteContext(siteCode);            
            var businesUnitType = siteContext.BusinessUnit;
            var isLtl = businesUnitType == BusinessUnitType.LTL;

            //var transactionTypes = _transactionTypeService.List(siteCode, null)            
            //let the code crash if the sitecode is not cached so we catch problems early

            var transactionTypes = _cachedLookupService.GetTransactionTypes(siteCode)
               .Flatten()
               .Select(r => new EfficiencyTransactionType()
               {
                   Code = r.Code,
                   Measured = r.Measured
               })
               .ToArray();

            //var workCenters = siteContext.GetWorkcenterService().ListIncludeInvalids(siteCode);                        
            //let the code crash if the sitecode is not cached so we catch problems early
            var workCenters = _cachedLookupService.GetWorkcenters(siteCode);
            
            foreach (var timeSheet in timeSheets)
            {
                var workCenter = workCenters.FirstOrDefault(x => x.Code.IgnoreCaseEquals(timeSheet.WorkedWorkCenterCode));
                timeSheet.WorkedWorkCenterName = workCenter?.Name;
                timeSheet.IsTransactionalWorkCenter = workCenter?.IsTransactional;
                timeSheet.WorkcenterType = workCenter?.WorkcenterType;
            }

            //var employeeShiftSupevisors = _shiftSupervisorService.List(siteCode);
            //let the code crash if the sitecode is not cached so we catch problems early
            var employeeShiftSupevisors = _cachedLookupService.GetEmployeeShiftSupevisors(siteCode);

            var timeSheetShifts = timeSheets.MapTimeSheetShifts(siteCode, siteNow, employees, employeeShiftSupevisors, businesUnitType, siteEmployeeCodes, transactionTypes);            
            var orphanShifts = GetOrphanedShiftsFromTransactions(siteCode, employees, isLtl, startTime.AddHours(-12), endTime.AddHours(12), extendedTimeSheets, transactionTypes, businesUnitType, siteEmployeeCodes, employeeShiftSupevisors);

            return timeSheetShifts.Concat(orphanShifts);
        }

        /// <summary>
        /// Returns a set of shifts for transactions that do not fall within the set of knownTimeSheets.
        /// </summary>
        /// <param name="siteCode"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="knownTimeSheets"></param>
        /// <param name="siteEmployeeCodesFilter"></param>
        /// <returns></returns>
        [NotNull]
        private IEnumerable<EfficiencyShift> GetOrphanedShiftsFromTransactions(
            [NotNull] string siteCode, [NotNull] Employee[] employees, bool isLtl, DateTime startTime, DateTime endTime,
            [NotNull] ICollection<EfficiencyTimeSheet> knownTimeSheets, 
            EfficiencyTransactionType[] transactionTypes, 
            BusinessUnitType businessUnitType, 
            [CanBeNull] IEnumerable<string> siteEmployeeCodesFilter = null, 
            [CanBeNull] EmployeeShiftSupevisor[] employeeShiftSupevisors = null)
        {
            var results = new List<EfficiencyShift>();
            
            var transactionCacheRepository = _transactionCacheRepositoryFactory.GetCurrent();
            var filteredSiteEmployeeCodes = siteEmployeeCodesFilter?.ToArray() ?? transactionCacheRepository.GetTransactionSiteEmployeeCodes(siteCode).ToArray();

            var mappedSiteEmployeeCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Mapped employees. Bucket everything by the EmployeeNumber.
            foreach (var employee in employees)
            {
                var siteEmployeeCodes = employee.SiteEmployees.Select(e => e.SiteEmployeeCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

                var employeeShiftSupevisor = employeeShiftSupevisors?.FirstOrDefault(x => x.Employee.TnaEmployeeCode.IgnoreCaseEquals(employee.TnaEmployeeCode));

                foreach (var siteEmployeeCode in filteredSiteEmployeeCodes)
                {
                    if (siteEmployeeCode.ExistsInArray(siteEmployeeCodes))
                        mappedSiteEmployeeCodes.AddIfNotContains(siteEmployeeCode);
                }

                if(!filteredSiteEmployeeCodes.Any(x => x.ExistsInArray(siteEmployeeCodes)))
                    continue;

                var orphanedShifts = GetOrphanedShifts(siteCode, isLtl, siteEmployeeCodes, knownTimeSheets.Where(x => x.TnaEmployeeCode.IgnoreCaseEquals(employee.TnaEmployeeCode)),
                    startTime, endTime, transactionTypes, businessUnitType, employee, employeeShiftSupevisor);
                results.AddRange(orphanedShifts.Where(x => Math.Abs((x.EndTime - x.StartTime).TotalHours) <= 24));

                var orphanShiftsToSplit = orphanedShifts.Where(x => Math.Abs((x.EndTime - x.StartTime).TotalHours) > 24);

                AddMultiDayShifts(orphanShiftsToSplit, results, _mapper);
            }

            // Unmapped employees. Bucket everything under the SiteEmployeeCode.
            foreach (var siteEmployeeCode in filteredSiteEmployeeCodes)
            {
                if (mappedSiteEmployeeCodes.Contains(siteEmployeeCode))
                {
                    // Already mapped
                    continue;
                }

                var orphanedShifts = GetOrphanedShifts(siteCode, isLtl, new[] { siteEmployeeCode },
                    new EfficiencyTimeSheet[0], startTime, endTime, transactionTypes, businessUnitType);

                results.AddRange(orphanedShifts.Where(x => Math.Abs((x.EndTime - x.StartTime).TotalHours) <= 24));

                var orphanShiftsToSplit = orphanedShifts.Where(x => Math.Abs((x.EndTime - x.StartTime).TotalHours) > 24);
                AddMultiDayShifts(orphanShiftsToSplit, results, _mapper);
            }

            return results;
        }

        private void AddMultiDayShifts(IEnumerable<EfficiencyShift> shifts, List<EfficiencyShift> results, Mapper mapper)
        {
            foreach (var item in shifts)
            {
                var end = item.EndTime;
                while (end.Date.AddDays(1) >= item.StartTime.Date.AddDays(1) && item.StartTime != end)
                {
                    item.EndTime = new DateTime(Math.Min(item.StartTime.Date.AddDays(1).Ticks, end.Ticks));
                    var newShift = mapper.Map<EfficiencyShift>(item);
                    newShift.OperationalDate = newShift.StartTime.Date;
                    results.Add(newShift);
                    item.StartTime = item.EndTime;
                }
            }
        }

        [NotNull]
        private IEnumerable<EfficiencyShift> GetOrphanedShifts([NotNull] string siteCode, bool isLtl, [NotNull] IReadOnlyCollection<string> siteEmployeeCodes,
            [NotNull] IEnumerable<EfficiencyTimeSheet> knownTimeSheets, DateTime startTime, DateTime endTime, EfficiencyTransactionType[] transactionTypes,
            BusinessUnitType businessUnitType, [CanBeNull] Employee employee = null, [CanBeNull] EmployeeShiftSupevisor employeeShiftSupevisor = null)
        {
            var transactionCacheRepository = _transactionCacheRepositoryFactory.GetCurrent();
            var transactions = transactionCacheRepository.GetTransactionsByDateRange(siteCode, siteEmployeeCodes, startTime, endTime);

            EfficiencyShift ShiftFactory() => new EfficiencyShift(employee, isLtl, siteCode, siteEmployeeCodes, transactionTypes, businessUnitType, employeeShiftSupevisor);

            return TimeSheetShiftHelpers.GetOrphanShifts(ShiftFactory, knownTimeSheets, transactions.OrderBy(t => t.TransactionDate));
        }
    }
}
