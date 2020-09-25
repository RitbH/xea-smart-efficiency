using JetBrains.Annotations;
using System;
using System.Linq;
using Xpo.Smart.Core.Models;
using Xpo.Smart.Efficiency.Shared.Extensions;
using Xpo.Smart.Efficiency.Shared.Extensions.Enums;

namespace Xpo.Smart.Efficiency.Shared.Models
{
    public abstract class EfficiencyRecordType
    {
        private readonly EfficiencyTransactionType[] _transactionTypes;
        private readonly bool _isEmployeeTransactional;
        private readonly WorkcenterType _workcenterType;
        private readonly bool _hasTimesheet;
        private readonly BusinessUnitType _businessUnitType;
        private readonly bool _isRecordTypeComputed;

        public EfficiencyRecordType([NotNull]EfficiencyShift shift, string transactionTypeCode)
        {
            _transactionTypes = shift.TransactionTypes;
            _isEmployeeTransactional = shift.IsEmployeeTransactional;
            _workcenterType = shift.WorkcenterType;
            _hasTimesheet = shift.TimeSheetId.HasValue;
            _businessUnitType = shift.BusinessUnitType;
            TransactionTypeCode = transactionTypeCode;
            _isRecordTypeComputed = true;
        }

        public EfficiencyRecordType()
        {
            _isRecordTypeComputed = false;
        }

        public string TransactionTypeCode { get; set; }

        public RecordType RecordType
        {
            get
            {
                if (_isRecordTypeComputed == false)
                    throw new NotImplementedException("RecordType is not supported without EfficiencyShift set up.");

                var isTransactionTypeMeasured = string.IsNullOrWhiteSpace(TransactionTypeCode)
                    ? true
                    : _transactionTypes
                        .Where(r => r.Code.IgnoreCaseEquals(TransactionTypeCode))
                        .Select(r => r.Measured)
                        .FirstOrDefault();

                return GetEfficiencyRecordType(_businessUnitType, _hasTimesheet, _isEmployeeTransactional, _workcenterType, isTransactionTypeMeasured);
            }
        }

        public RecordType BuildRecordType(string transactionTypeCode)
        {
            var isTransactionTypeMeasured = string.IsNullOrWhiteSpace(transactionTypeCode)
                ? true
                : _transactionTypes
                    .Where(r => r.Code.IgnoreCaseEquals(transactionTypeCode))
                    .Select(r => r.Measured)
                    .FirstOrDefault();

            return GetEfficiencyRecordType(_businessUnitType, _hasTimesheet, _isEmployeeTransactional, _workcenterType, isTransactionTypeMeasured);
        }

        private static RecordType GetEfficiencyRecordType(
            BusinessUnitType businessUnitType,
            bool hasTimesheet,
            bool isEmployeeTransactional,
            WorkcenterType workcenterType,
            bool isTransactionTypeMeasured)
        {
            if (hasTimesheet == false && businessUnitType == BusinessUnitType.LTL)
                return RecordType.NonTransactional;
            if (isEmployeeTransactional == false)
                return RecordType.NonTransactional;
            if (workcenterType == WorkcenterType.NON_TRANSACTIONAL)
                return RecordType.NonTransactional;
            if (workcenterType == WorkcenterType.MONITORED)
                return RecordType.Monitored;
            if (isTransactionTypeMeasured == false)
                return RecordType.Monitored;

            return RecordType.Transactional;
        }
    }
}
