using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 71776;
        public Ptr UnitHashTable => 0x227B5C0;
        public Ptr SessionData => 0x2469198;
        public Ptr Pets => 0x2296BD8;
        public Ptr UIState => 0x228b2a8;
        public Ptr IsOnlineGame => 0x2136D50;
        public Ptr WidgetStates => 0x22B3810;
        public Ptr CharFlags => 0x212C3CB;
    }
}
