using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 70569;
        public Ptr UnitHashTable => 0x224ECA0;
        public Ptr SessionData => 0x243C518;
        public Ptr Pets => 0x226a3a0;
        public Ptr UIState => 0x225e978;
        public Ptr IsOnlineGame => 0x210a650;
        public Ptr WidgetStates => 0x2286e18;
        public Ptr CharFlags => 0x20ffccb;
    }
}
