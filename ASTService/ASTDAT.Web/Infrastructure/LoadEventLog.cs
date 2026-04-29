using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;

namespace ASTDAT.Web.Infrastructure
{
    /// <summary>Lightweight in-process event bus for the UI to poll (GET api/LoadEvents/Since). Replace/augment with SignalR for true push.</summary>
    public static class LoadEventLog
    {
        static readonly object LockObj = new object();
        static long _nextId;
        const int MaxEvents = 800;
        static readonly List<LoadEventEntry> Store = new List<LoadEventEntry>();

        public static long Append(string type, JObject data)
        {
            if (string.IsNullOrEmpty(type) || data == null) return 0;
            lock (LockObj)
            {
                _nextId++;
                var e = new LoadEventEntry
                {
                    Id = _nextId,
                    Type = type,
                    Data = data,
                    Utc = DateTime.UtcNow
                };
                Store.Add(e);
                while (Store.Count > MaxEvents)
                    Store.RemoveAt(0);
                return _nextId;
            }
        }

        public static long Append(string type, object payload) =>
            Append(type, JObject.FromObject(payload ?? new { }));

        public static IReadOnlyList<LoadEventDto> GetAfter(long afterId)
        {
            lock (LockObj)
            {
                return Store
                    .Where(x => x.Id > afterId)
                    .Select(x => new LoadEventDto { Id = x.Id, Utc = x.Utc, Type = x.Type, Data = x.Data })
                    .ToList();
            }
        }
    }

    public sealed class LoadEventEntry
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public JObject Data { get; set; }
        public DateTime Utc { get; set; }
    }

    public sealed class LoadEventDto
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public JObject Data { get; set; }
        public DateTime Utc { get; set; }
    }
}
