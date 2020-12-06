using FluentMigrator;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcCore.Data
{
    [NopMigration("2020/12/06 11:44:55:1687541", "Misc.AbcMattresses base schema")]
    public class SchemaMigration : AutoReversingMigration
    {
        protected IMigrationManager _migrationManager;

        public SchemaMigration(IMigrationManager migrationManager)
        {
            _migrationManager = migrationManager;
        }

        public override void Up()
        {
            _migrationManager.BuildTable<AbcMattress>(Create);
            _migrationManager.BuildTable<AbcMattressEntry>(Create);
            _migrationManager.BuildTable<AbcMattressGift>(Create);
            _migrationManager.BuildTable<AbcMattressBase>(Create);
            _migrationManager.BuildTable<AbcMattressPackage>(Create);
        }
    }
}
