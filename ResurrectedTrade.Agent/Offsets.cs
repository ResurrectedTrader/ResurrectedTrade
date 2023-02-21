using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 73090;
        public Ptr UnitHashTable => 0x2282D80;
        public Ptr SessionData => 0x2471958;
        public Ptr Pets => 0x229E490;
        public Ptr UIState => 0x2292A60;
        public Ptr IsOnlineGame => 0x0FDDD0;
        public Ptr WidgetStates => 0x22BAFC8;
        public Ptr CharFlags => 0x0F343B;
    }
}
