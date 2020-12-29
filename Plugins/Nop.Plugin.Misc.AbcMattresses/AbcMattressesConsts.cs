using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses
{
    public static class AbcMattressesConsts
    {
        private static string[] _bases = new string[]
        {
            BaseNameTwin, BaseNameTwinXL, BaseNameFull, BaseNameQueen, BaseNameKing, BaseNameCaliforniaKing
        };
        private static string[] _mattressProtectors = new string[]
        {
            MattressProtectorTwin,
            MattressProtectorTwinXL,
            MattressProtectorFull,
            MattressProtectorQueen,
            MattressProtectorKing,
            MattressProtectorCaliforniaKing
        };

        public static bool IsBase(string value)
        {
            return _bases.Contains(value);
        }
        public static bool IsMattressProtector(string value)
        {
            return _mattressProtectors.Contains(value);
        }

        public const string MattressSizeName = "Mattress Size";
        public const string BaseNameTwin = "Base (Twin)";
        public const string BaseNameTwinXL = "Base (TwinXL)";
        public const string BaseNameFull = "Base (Full)";
        public const string BaseNameQueen = "Base (Queen)";
        public const string BaseNameKing = "Base (King)";
        public const string BaseNameCaliforniaKing = "Base (California King)";
        public const string MattressProtectorTwin = "Mattress Protector (Twin)";
        public const string MattressProtectorTwinXL = "Mattress Protector (TwinXL)";
        public const string MattressProtectorFull = "Mattress Protector (Full)";
        public const string MattressProtectorQueen = "Mattress Protector (Queen)";
        public const string MattressProtectorKing = "Mattress Protector (King)";
        public const string MattressProtectorCaliforniaKing = "Mattress Protector (California King)";
        public const string FreeGiftName = "Free Gift";

        public const string Twin = "Twin";
        public const string TwinXL = "Twin XL";
        public const string Full = "Full";
        public const string Queen = "Queen";
        public const string King = "King";
        public const string CaliforniaKing = "California King";
    }
}