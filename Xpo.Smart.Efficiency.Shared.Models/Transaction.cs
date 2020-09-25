using System;

namespace Xpo.Smart.Efficiency.Shared.Models
{
    public class Transaction
    {
        public long TransactionId { get; set; }

        public long? PreviousTransactionId { get; set; }

        public string SiteCode { get; set; }

        public string SiteEmployeeCode { get; set; }

        public string TransactionTypeCode { get; set; }

        public decimal? Quantity { get; set; }

        public decimal? QuantityEarned { get; set; }

        public decimal? QuantityProcessed { get; set; }

        public decimal? DuplicateQuantity => (QuantityProcessed - QuantityEarned) ?? 0;

        public int TransitionTimeSeconds { get; set; }

        public int SecondsEarned { get; set; }

        public DateTime OperationalDate { get; set; }

        public DateTime TransactionDate { get; set; }

        public string PreviousTransactionTypeCode { get; set; }

        public string SegmentCode { get; set; }

        /// <summary>
        /// Gets whether or not this transaction is a transition.
        /// Transactions without a previous transaction are not counted as functional transitions.
        /// </summary>
        public bool IsFunctionTransition => !string.IsNullOrWhiteSpace(TransactionTypeCode) &&
                                            !string.IsNullOrWhiteSpace(PreviousTransactionTypeCode) &&
                                            !string.Equals(PreviousTransactionTypeCode, TransactionTypeCode);
    }
}
