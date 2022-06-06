using ResurrectedTrade.AgentBase;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.Agent
{
    public class Offsets : IOffsets
    {
        public static readonly Offsets Instance = new Offsets();

        public int SupportedVersion => 69807;
        public Ptr UnitHashTable => 0x226C6C0;
        public Ptr SessionData => 0x2459B78;
        public Ptr Pets => 0x2287CB8;
        public Ptr UIState => 0x227C398;
        public Ptr IsOnlineGame => 0x20E7ED0;
        public Ptr WidgetStates => 0x22A4838;
        public Ptr CharFlags => 0x20DD56B;
    }
}
