using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 69640;
        public Ptr UnitHashTable => 0x224B660;
        public Ptr SessionData => 0x2438B08;
        public Ptr Pets => 0x2266C58;
        public Ptr UIState => 0x225B338;
        public Ptr IsOnlineGame => 0x2107250;
        public Ptr WidgetStates => 0x22837D8;
        public Ptr CharFlags => 0x20FC8DB;
    }
}
