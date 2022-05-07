using System;

namespace ResurrectedTrade.Common.Enums
{
    [Flags]
    public enum CharFlag : uint
    {
        Hardcore = 4,
        DiedBefore = 8,
        Expansion = 32,
        Ladder = 64
    }
}