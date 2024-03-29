namespace Nop.Plugin.Misc.AbcCore
{
    public class CoreLocales
    {
        public const string Base = "Plugins.Misc.AbcCore.Fields.";

        public const string BackendDbConnectionString =
            Base + "BackendDbConnectionString";
        public const string BackendDbConnectionStringHint =
            BackendDbConnectionString + ".Hint";

        public const string AreExternalCallsSkipped =
            Base + "AreExternalCallsSkipped";
        public const string AreExternalCallsSkippedHint =
            AreExternalCallsSkipped + ".Hint";

        public const string IsDebugMode =
            Base + "IsTraceMode";
        public const string IsDebugModeHint =
            IsDebugMode + ".Hint";

        public const string FlixId =
            Base + "FlixId";
        public const string FlixIdHint =
            FlixId + ".Hint";

        public const string PLPDescription =
            Base + "PLPDescription";
        public const string PLPDescriptionHint =
            PLPDescription + ".Hint";
    }
}