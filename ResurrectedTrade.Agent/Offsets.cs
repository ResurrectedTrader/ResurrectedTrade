using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 77312; //11.14.2023
        public Ptr UnitHashTable => 0x22F20C0;
        public Ptr SessionData => 0x24E0E78;
        public Ptr Pets => 0x230D7D0;
        public Ptr InGame => 0x2301DA0;
        public Ptr IsOnlineGame => 0x21AD2D0;
        public Ptr WidgetStates => 0x232A308;
        public Ptr CharFlags => 0x21A293B;
        public Ptr LoadGameComplete => 0x21A4EAC;
    }
}
