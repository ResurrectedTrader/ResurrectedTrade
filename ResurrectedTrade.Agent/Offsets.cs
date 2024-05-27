using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 80273;
        public Ptr UnitHashTable => 0x22DA110;
        public Ptr SessionData => 0x24C8E60;
        public Ptr Pets => 0x22F5728;
        public Ptr InGame => 0x22E9DF8;
        public Ptr IsOnlineGame => 0x2154F50;
        public Ptr WidgetStates => 0x2312360;
        public Ptr CharFlags => 0x214A5BB;
        public Ptr LoadGameComplete => 0x214CB48;
    }
}
