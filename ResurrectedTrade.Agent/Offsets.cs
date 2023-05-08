using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 74264;
        public Ptr UnitHashTable => 0x228F080;
        public Ptr SessionData => 0x247DC68;
        public Ptr Pets => 0x22AA790;
        public Ptr UIState => 0x229ED60;
        public Ptr IsOnlineGame => 0x2109ED0;
        public Ptr WidgetStates => 0x22C72C8;
        public Ptr CharFlags => 0x20FF53B;
    }
}
