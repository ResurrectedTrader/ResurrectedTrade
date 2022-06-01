using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 69754;
        public Ptr UnitHashTable => 0x226F6C0;
        public Ptr SessionData => 0x245CB98;
        public Ptr Pets => 0x228ACB8;
        public Ptr UIState => 0x227F398;
        public Ptr IsOnlineGame => 0x20EAED0;
        public Ptr WidgetStates => 0x22A7838;
        public Ptr CharFlags => 0x20E055B;
    }
}
