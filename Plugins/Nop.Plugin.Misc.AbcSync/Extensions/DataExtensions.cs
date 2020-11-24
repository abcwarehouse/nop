namespace Nop.Plugin.Misc.AbcSync.Extensions
{
    public static class DataExtensions
    {
        public static string GetDatabaseName(this string connectionString)
        {
            var start =
                connectionString.Substring(connectionString.IndexOf(";Initial Catalog=") + 17);
            
            return start.Substring(0, start.IndexOf(";"));
        }
    }
}