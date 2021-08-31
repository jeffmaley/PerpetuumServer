using Perpetuum.Data;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences.RandomExpiringPresence;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence
{

    public interface IEscalatingPresenceFlockSelector
    {
        IFlockConfiguration[] GetFlocksForPresenceLevel(GrowingPresence presence, int level);
    }
    public class EscalatingPresenceFlockSelector : IEscalatingPresenceFlockSelector
    {
        private readonly EscalatingFlocksReader _reader;
        public EscalatingPresenceFlockSelector(EscalatingFlocksReader reader)
        {
            _reader = reader;
        }
        public IFlockConfiguration[] GetFlocksForPresenceLevel(GrowingPresence presence, int level)
        {
            if (level < 0 || level > presence.GrowthLevel)
            {
                return new IFlockConfiguration[] { };
            }
            var infos = _reader.GetByPresence(presence);
            var flocks = new List<IFlockConfiguration>();
            foreach(var info in infos)
            {
                if(info.Chance >= FastRandom.NextDouble())
                {
                    flocks.Add(presence.FlockConfigurationRepository.Get(info.FlockId));
                }
            }
            return flocks.ToArray();
        }
    }

    public class EscalationInfo
    {
        public int FlockId { get; set; }
        public int Level { get; set; }
        public double Chance { get; set; }
    }


    public class EscalatingFlocksReader
    {
        private ILookup<int, EscalationInfo> _flockInfos;

        public void Init()
        {
            _flockInfos = Db.Query().CommandText("select * from npcescalations")
                .Execute()
                .Select(r =>
                {
                    return new
                    {
                        presenceID = r.GetValue<int>("presenceid"),
                        info = new EscalationInfo
                        {
                            FlockId = r.GetValue<int>("flockid"),
                            Level = r.GetValue<int>("level"),
                            Chance = r.GetValue<double>("chance")
                        }
                    };
                }).ToLookup(x => x.presenceID, x => x.info);
        }

        public EscalationInfo[] GetByPresence(Presence presence)
        {
            return _flockInfos.GetOrEmpty(presence.Configuration.ID);
        }
    }
}
