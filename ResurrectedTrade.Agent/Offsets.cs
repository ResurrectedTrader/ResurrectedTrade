using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 75728;
        public Ptr UnitHashTable => 0x22a7000;
        public Ptr SessionData => 0x2495bf8;
        public Ptr Pets => 0x22c2618;
        public Ptr InGame => 0x22B6CE8;
        public Ptr IsOnlineGame => 0x2162250;
        public Ptr WidgetStates => 0x22df250;
        public Ptr CharFlags => 0x21578bb;
        public Ptr LoadGameComplete => 0x2159E48;
    }
}
