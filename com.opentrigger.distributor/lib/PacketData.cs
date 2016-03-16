using System;
using System.Collections.Generic;
using System.Linq;

namespace com.opentrigger.distributord
{
    public class PacketData
    {
        public DateTimeOffset Timestamp { get; set; }
        public byte[] Packet { get; set; }
        public string UniqueIdentifier { get; set; }
        public string OriginTopic { get; set; }
    }

    public class ReleaseData : PacketData
    {
        public int Age { get; set; }
    }

    public class PacketFilter
    {
        public Action<PacketData> OnTrigger = data => { };
        public Action<ReleaseData> OnRelease = data => { };
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
            lock (_lock)
            {
                var existing = _allData.Count(d => d.UniqueIdentifier == data.UniqueIdentifier);
                _allData.Add(data);
                if (existing - _skip == 0)
                {
                    if(_verbosity>=3) Console.WriteLine($"TAKEING: {data.UniqueIdentifier}");
                    var alreadyDown = _downList.Count(d => d.UniqueIdentifier == data.UniqueIdentifier);
                    if (alreadyDown == 0)
                    {
                        _downList.Add(data);
                        OnTrigger(data);
                    }
                }
                else
                {
                    if (_verbosity >= 3 && existing - _skip < 0) Console.WriteLine($"SKIPING: {data.UniqueIdentifier}");
                    if (_verbosity >= 4 && existing - _skip > 0) Console.WriteLine($"DUPLICA: {data.UniqueIdentifier}");
                }
            }
        }

        public void Cleanup()
        {
            lock (_lock)
            {
                var limit = DateTime.UtcNow.AddMilliseconds(_distance * -1);
                _allData.RemoveAll(d => d.Timestamp < limit);

                var uniqueMacs = _allData.Select(d => d.UniqueIdentifier).Distinct();
                var tailing = _downList.Where(d => !uniqueMacs.Contains(d.UniqueIdentifier));
                foreach (var data in tailing)
                {
                    OnRelease(new ReleaseData
                    {
                        UniqueIdentifier = data.UniqueIdentifier,
                        Packet = data.Packet,
                        Timestamp = data.Timestamp,
                        OriginTopic = data.OriginTopic,
                        Age = (int)(DateTimeOffset.UtcNow - data.Timestamp).TotalMilliseconds,
                    });
                }
                _downList.RemoveAll(d => !uniqueMacs.Contains(d.UniqueIdentifier));
            }
        }
    }
}
