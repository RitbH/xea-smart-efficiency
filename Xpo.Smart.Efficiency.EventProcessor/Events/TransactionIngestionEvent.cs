using System;
using System.Runtime.Serialization;
using Xpo.Common.Cqrs;
using Xpo.Smart.Contracts.Primitives;
using Transaction = Xpo.Smart.Core.Models.Canonical.Transactions.Transaction;

namespace Xpo.Smart.Efficiency.EventProcessor.Events
{
    /// <summary>
    /// Represents the TransactionIngestionEvent contract as an event
    /// </summary>
    [EventContract(Service = Constants.Service, Name = nameof(TransactionIngestionEvent))]
    public sealed class TransactionIngestionEvent : IEvent, IIngestionModel<Transaction>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionIngestionEvent"/> class.
        /// </summary>
        public TransactionIngestionEvent()
        {
            ReceivedTimestamp = DateTime.UtcNow;
        }

        public TransactionIngestionEvent(IIngestionModel<Transaction> source)
        {
            Key = source.Key;
            Value = source.Value;
            IsDeleted = source.IsDeleted;
            ReceivedTimestamp = source.ReceivedTimestamp;
            ExtendedProperties = source.ExtendedProperties;
            EventCategoryType = source.EventCategoryType;
        }

        /// <summary>
        /// Gets or sets the key to uniquely identify an Order Line Item record.
        /// </summary>
        /// <value>
        /// The key to uniquely identify an Order Line Item record.
        /// </value>

        [DataMember(Order = 1)]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value of the order line item record.
        /// </summary>
        /// <value>
        /// The value of the order line item record.
        /// </value>
        [DataMember(Order = 2)]
        public Transaction Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the order line item record is deleted.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the order line item record is deleted; otherwise, <c>false</c>.
        /// </value>
        [DataMember(Order = 3)]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets the timestamp in which this instance was received.
        /// </summary>
        /// <value>
        /// The timestamp in which this instance was received.
        /// </value>
        [DataMember(Order = 4)]
        public DateTimeOffset ReceivedTimestamp { get; set; }

        /// <summary>
        /// Gets extra properties of the model that are not part of the canonical one.
        /// </summary>
        /// <value>
        /// The extended properties of the model.
        /// </value>
        [DataMember(Order = 5)]
        public ExtendedProperties ExtendedProperties { get; set; }

        [DataMember(Order = 6)] public string PartitioningKey => Key;

        /// <summary>
        /// Event Category of this event
        /// </summary>
        [DataMember(Order = 7)]
        public EventCategoryType? EventCategoryType { get; set; }
    }
}
