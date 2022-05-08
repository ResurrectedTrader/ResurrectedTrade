using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 69324;
        public Ptr UnitHashTable => 0x2237560;
        public Ptr SessionData => 0x2424A28;
        public Ptr Pets => 0x2252B58;
        public Ptr UIState => 0x2247238;
        public Ptr IsOnlineGame => 0x20F3150;
        public Ptr WidgetStates => 0x226F6D8;
        public Ptr CharFlags => 0x20E87F3;
    }
}
