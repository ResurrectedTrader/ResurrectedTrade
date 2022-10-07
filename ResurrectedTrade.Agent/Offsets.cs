using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 71510;
        public Ptr UnitHashTable => 0x22875c0;
        public Ptr SessionData => 0x2474FB8;
        public Ptr Pets => 0x22a2bd8;
        public Ptr UIState => 0x22972a8;
        public Ptr IsOnlineGame => 0x2142d50;
        public Ptr WidgetStates => 0x22bf810;
        public Ptr CharFlags => 0x21383cb;
    }
}
