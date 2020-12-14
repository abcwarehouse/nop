using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses
{
    public static class AbcMattressesConsts
    {
        private static string[] _bases = new string[]
        {
            BaseNameTwin, BaseNameTwinXL, BaseNameFull, BaseNameQueen, BaseNameKing, BaseNameCaliforniaKing
        };

        public static bool IsBase(string value)
        {
            return _bases.Contains(value);
        }

        public const string MattressSizeName = "Mattress Size";
        public const string BaseNameTwin = "Base (Twin)";
        public const string BaseNameTwinXL = "Base (TwinXL)";
        public const string BaseNameFull = "Base (Full)";
        public const string BaseNameQueen = "Base (Queen)";
        public const string BaseNameKing = "Base (King)";
        public const string BaseNameCaliforniaKing = "Base (California King)";
        public const string FreeGiftName = "Free Gift";

        public const string Twin = "Twin";
        public const string TwinXL = "Twin XL";
        public const string Full = "Full";
        public const string Queen = "Queen";
        public const string King = "King";
        public const string CaliforniaKing = "California King";
    }
}