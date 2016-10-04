using System;
using System.Collections.Generic;
using System.Linq;

namespace com.opentrigger.distributord
{
    
    public class PacketBase
    {
        public DateTimeOffset Timestamp { get; set; }
        public string UniqueIdentifier { get; set; }
        public string Origin { get; set; }
        public EventType EventType { get; set; }
        public int? EventId { get; set; }
    }

    public class PacketData : PacketBase
    {
        public BtleDecoded Packet { get; set; }
    }

    public class ReleaseData : PacketData
    {
        public int Age { get; set; }
    }

    public class UniqueIdentifierComparer : IEqualityComparer<PacketData>, IComparer<PacketData>
    {
        public bool Equals(PacketData x, PacketData y)
        {
            return string.Equals(x.UniqueIdentifier, y.UniqueIdentifier, StringComparison.Ordinal);
            //return x.UniqueIdentifier == y.UniqueIdentifier;
        }

        public int GetHashCode(PacketData obj) => obj.UniqueIdentifier.GetHashCode();
        public int Compare(PacketData x, PacketData y) => string.CompareOrdinal(x.UniqueIdentifier, y.UniqueIdentifier);
    }


    public class PacketFilter
    {
        public Action<PacketData> OnTrigger = data => { };
        public Action<ReleaseData> OnRelease = data => { };
        public Action<string> OnLogentry = logline => { Console.WriteLine(logline); };
        private readonly List<PacketData> _allData = new List<PacketData>();
        private readonly List<PacketData> _downList = new List<PacketData>();
        private readonly object _lock = new object();
        private readonly int _skip;
        private readonly int _distance;
        private readonly int _verbosity;

        public PacketFilter(int skip = 0, int distance = 2000, int verbosity = 0)
        {
            _skip = skip;
            _distance = distance;
            _verbosity = verbosity;
        }

        public void Add(PacketData data)
        {
            if(data.Timestamp == default(DateTimeOffset)) data.Timestamp = DateTimeOffset.UtcNow;
            // if (_skip == 0 && _distance == 0) -- TODO: shortcut for 'no deduplication'
            lock (_lock)
            {
                var existing = _allData.Count(d => d.UniqueIdentifier == data.UniqueIdentifier);
                _allData.Add(data);
                if (existing - _skip == 0)
                {
                    if(_verbosity>=3) OnLogentry($"TAKEING: {data.UniqueIdentifier}");
                    var alreadyDown = _downList.Count(d => d.UniqueIdentifier == data.UniqueIdentifier);
                    if (alreadyDown == 0)
                    {
                        _downList.Add(data);
                        data.EventId = data.Packet.GetEventId();
                        OnTrigger(data);
                    }
                }
                else
                {
                    if (_verbosity >= 3 && existing - _skip < 0) OnLogentry($"SKIPING: {data.UniqueIdentifier}");
                    if (_verbosity >= 4 && existing - _skip > 0)
                    {
                        var oldestExisting = _allData.Where(d=>d.UniqueIdentifier == data.UniqueIdentifier).OrderByDescending(d=>d.Timestamp).Last().Timestamp;
                        OnLogentry($"DUPLICA: {data.UniqueIdentifier} tOff:{(data.Timestamp - oldestExisting).TotalMilliseconds:00000}");
                    }
                }
            }
            WorkCycle();
        }

        private readonly UniqueIdentifierComparer _uidComparer = new UniqueIdentifierComparer();
        public void WorkCycle()
        {
            lock (_lock)
            {
                var limit = DateTime.UtcNow.AddMilliseconds(_distance * -1);
                _allData.RemoveAll(d => d.Timestamp < limit);
                var uniqueUids = _allData.Distinct(_uidComparer);
                var tailing = _downList.Where(d => !uniqueUids.Contains(d,_uidComparer));
                foreach (var data in tailing)
                {
                    data.Packet.GetEventId();
                    OnRelease(new ReleaseData
                    {
                        UniqueIdentifier = data.UniqueIdentifier,
                        Packet = data.Packet,
                        Timestamp = data.Timestamp,
                        Origin = data.Origin,
                        Age = (int)(DateTimeOffset.UtcNow - data.Timestamp).TotalMilliseconds,
                        EventId = data.Packet.GetEventId()
                    });
                }
                _downList.RemoveAll(d => !uniqueUids.Contains(d,_uidComparer));
            }
        }

        public int RemoveByUniqueIdentifier(string uniqueIdentifier) => RemoveByUniqueIdentifiers(new [] {uniqueIdentifier});
        public int RemoveByUniqueIdentifiers(IEnumerable<string> uniqueIdentifiers)
        {
            lock (_lock)
            {
                return _allData.RemoveAll(d => uniqueIdentifiers.Contains(d.UniqueIdentifier));
            }
        }
    }
}
