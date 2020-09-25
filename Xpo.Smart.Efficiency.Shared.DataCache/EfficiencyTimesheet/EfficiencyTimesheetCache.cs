using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using Xpo.Smart.Core.DataCache;
using Xpo.Smart.Efficiency.Shared.Models;

namespace Xpo.Smart.Efficiency.Shared.DataCache.EfficiencyTimesheet
{
    public class EfficiencyTimesheetCache: DataCache<EfficiencyTimeSheet>
    {
        private readonly static IMapper _mapper;
        
        private readonly TimeSpan _lowerBound;
        private readonly TimeSpan _upperBound;

        static EfficiencyTimesheetCache()
        {
            _mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EfficiencyTimeSheet, CachedEfficiencyTimesheet>();
                cfg.CreateMap<CachedEfficiencyTimesheet, EfficiencyTimeSheet>();
            }).CreateMapper();
        }

        public EfficiencyTimesheetCache(TimeSpan lowerBound, TimeSpan upperBound)
        {
            _lowerBound = lowerBound;
            _upperBound = upperBound;
        }

        private readonly BucketCache<string, long, CachedEfficiencyTimesheet> _bucketCache = new BucketCache<string, long, CachedEfficiencyTimesheet>();

        protected override void AddOrUpdateItems(IEnumerable<EfficiencyTimeSheet> items)
        {
            //AND[t].[PunchInTime] < @EndTime
            //AND ISNULL([t].[PunchOutTime], @StartTime) >= @StartTime

            foreach (var item in items.GroupBy(r => r.SiteCode))
            {
                var toUpsert = item.Where(r => r.PunchInTime < HighestDate && (r.PunchOutTime ?? LowestDate) >= LowestDate);
                if (!toUpsert.Any())
                    continue;

                _bucketCache.AddOrUpdate(item.Key, toUpsert.Select(r => Map(r)));
            }
        }

        public void DeleteItems(IEnumerable<EfficiencyTimeSheet> items)
        {
            foreach (var item in items.GroupBy(r => r.SiteCode))
            {
                _bucketCache.Delete(item.Key, item.Select(r => r.TimeSheetId));
            }
        }

        public IEnumerable<string> GetSiteCodes()
        {
            return _bucketCache.GetKeys();
        }

        public IEnumerable<EfficiencyTimeSheet> GetAll(string siteCode)
        {
            return _bucketCache
                .GetAll(new string[] { siteCode })
                .Select(r => Map(r))
                .OrderBy(r=>r.PunchInTime);
        }

        protected override void PruneItems()
        {
            //AND[t].[PunchInTime] < @EndTime
            //AND ISNULL([t].[PunchOutTime], @StartTime) >= @StartTime

            var toDelete = _bucketCache.GetAll(_bucketCache.GetKeys())
                .Where(r => r.Item.PunchInTime > HighestDate || r.Item.PunchOutTime < LowestDate)
                .GroupBy(r => r.Key);

            foreach (var item in toDelete)
            {
                _bucketCache.Delete(item.Key, item.Select(r => r.Item.Key));
            }
        }

        public void Prune(string siteCode, IEnumerable<string> tnaEmployeeCodes)
        {
            
            var tnaEmployeeHash = new HashSet<string>(tnaEmployeeCodes.Distinct());

            var toDelete = _bucketCache.GetAll(siteCode)
                .Where(r => 
                    (r.Item.PunchInTime > HighestDate || r.Item.PunchOutTime < LowestDate) 
                    && tnaEmployeeHash.Contains (r.Item.TnaEmployeeCode))
                .GroupBy(r => r.Key);
            
            foreach (var item in toDelete)
            {
                _bucketCache.Delete(item.Key, item.Select(r => r.Item.Key));
            }
        }

        public DateTime LowestDate => DateTime.UtcNow.Add(_lowerBound);
        public DateTime HighestDate => DateTime.UtcNow.Add(_upperBound);

        private EfficiencyTimeSheet Map(BucketCacheItem<string, CachedEfficiencyTimesheet> item)
        {
            return _mapper.Map<EfficiencyTimeSheet>(item.Item);            
        }

        private CachedEfficiencyTimesheet Map(EfficiencyTimeSheet currentTimeGap)
        {
            return _mapper.Map<CachedEfficiencyTimesheet>(currentTimeGap);            
        }

        private CachedEfficiencyTimesheet DateBound(DateTime time) => new CachedEfficiencyTimesheet{ PunchInTime = time, TimeSheetId = 0 };
    }
}
