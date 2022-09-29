using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 71336;
        public Ptr UnitHashTable => 0x2252640;
        public Ptr SessionData => 0x2440078;
        public Ptr Pets => 0x226dd50;
        public Ptr UIState => 0x2262320;
        public Ptr IsOnlineGame => 0x210ddd0;
        public Ptr WidgetStates => 0x228a888;
        public Ptr CharFlags => 0x210344b;
    }
}
