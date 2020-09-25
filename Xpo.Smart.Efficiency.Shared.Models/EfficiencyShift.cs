using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.Models;

namespace Xpo.Smart.Efficiency.Shared.Models
{
    public class EfficiencyShift
    {
        public EfficiencyShift()
        {
        }

        private DateTime MinDate(DateTime siteNow, DateTime? punchOutTime)
        {
            if (!punchOutTime.HasValue)
                return siteNow;

            return new DateTime(Math.Min(siteNow.Ticks, punchOutTime.Value.Ticks));
        }

        public EfficiencyShift(
            [CanBeNull] Employee employee,
            EfficiencyTimeSheet timeSheet, 
            DateTime siteNow, 
            string[] siteEmployeeCodes, 
            [CanBeNull] EmployeeShiftSupevisor employeeShiftSupevisor, 
            EfficiencyTransactionType[] transactionTypes,
            BusinessUnitType businessUnitType) :
            this(
                employee, 
                employeeShiftSupevisor,
                siteEmployeeCodes,
                transactionTypes,
                businessUnitType)
        {            
            StartTime = timeSheet.PunchInTime;
            IsClockedIn = !timeSheet.PunchOutTime.HasValue || timeSheet.PunchOutTime > siteNow;
            EndTime = IsClockedIn ? MinDate(siteNow, timeSheet.PunchOutTime) : timeSheet.PunchOutTime.Value;
            OperationalDate = timeSheet.OperationalDate;
            

            ShiftCode = timeSheet.ShiftCode;
            SiteCode = timeSheet.SiteCode;            
            WorkedWorkCenterCode = timeSheet.WorkedWorkCenterCode;                                                                
            TimeSheetId = timeSheet.TimeSheetId;

            IsTransactional = employee?.IsTransactional == true && timeSheet.IsTransactionalWorkCenter != false;
            IsEmployeeTransactional = employee?.IsTransactional ?? true; //Chandra;
            WorkcenterType = timeSheet.WorkcenterType ?? WorkcenterType.TRANSACTIONAL; //Chandra                     
        }

        public EfficiencyShift(
            [CanBeNull] Employee employee, 
            bool isLtl,
            string siteCode,
            IReadOnlyCollection<string> siteEmployeeCodes,
            EfficiencyTransactionType[] transactionTypes,
            BusinessUnitType businessUnitType,
            [CanBeNull] EmployeeShiftSupevisor employeeShiftSupevisor = null) : 
            this(
                employee, 
                employeeShiftSupevisor,
                siteEmployeeCodes,
                transactionTypes,
                businessUnitType)
        {

            IsClockedIn = false;

            ShiftCode = null;
            SiteCode = siteCode;            
            WorkedWorkCenterCode = null;                                                       
            TimeSheetId = null;

            IsTransactional = !isLtl && (employee?.IsTransactional ?? true);
            IsEmployeeTransactional = !isLtl && (employee?.IsTransactional ?? true);
            WorkcenterType = WorkcenterType.TRANSACTIONAL;                 
        }

        private EfficiencyShift(
            [CanBeNull] Employee employee, 
            [CanBeNull] EmployeeShiftSupevisor employeeShiftSupevisor,
            IReadOnlyCollection<string> siteEmployeeCodes,
            [CanBeNull] EfficiencyTransactionType[] transactionTypes,
            BusinessUnitType businessUnitType)
        {
            EmployeeNumber = employee?.Number;
            EmployeeFullName = employee?.FullName;
            Supervisor = employee?.SupervisorFullName;            
            ShiftSupervisor = employeeShiftSupevisor?.Supervisor?.FullName;
            EmployeeJobCode = employee?.JobCode;
            SalaryClassCode = employee?.SalaryClassCode;
            IsPartTimeEmployee = employee?.IsPartTime ?? false;
            SiteEmployeeCodes = siteEmployeeCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            TransactionTypes = transactionTypes ?? new EfficiencyTransactionType[0];
            BusinessUnitType = businessUnitType;
        }


        public string EmployeeNumber { get; set; }

        public string EmployeeFullName { get; set; }

        public string EmployeeJobCode { get; set; }

        public string Supervisor { get; set; }

        public string ShiftSupervisor { get; set; }
        
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }
        
        public DateTime OperationalDate { get; set; }

        public string ShiftCode { get; set; }
        
        public string WorkedWorkCenterCode { get; set; }

        public string SiteCode { get; set; }
        
        public IReadOnlyCollection<string> SiteEmployeeCodes { get; set; }

        public bool IsTransactional { get; set; }

        public bool IsClockedIn { get; set; }

        public bool IsPartTimeEmployee { get; set; }

        public string SalaryClassCode { get; set; }

        public long? TimeSheetId { get; set; }
        
        public bool IsEmployeeTransactional { get; set; }

        public WorkcenterType WorkcenterType { get; set; }       

        public EfficiencyTransactionType[] TransactionTypes { get; set; }

        public BusinessUnitType BusinessUnitType { get; set; }
    }
}
