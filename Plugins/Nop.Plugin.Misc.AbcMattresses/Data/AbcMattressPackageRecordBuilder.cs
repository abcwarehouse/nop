using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcMattresses.Data
{
    public class AbcMattressPackageRecordBuilder : NopEntityBuilder<AbcMattressPackage>
    {
        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
            .WithColumn(nameof(AbcMattressPackage.MattressItemNo)).AsInt32()
            .WithColumn(nameof(AbcMattressPackage.BaseItemNo)).AsInt32()
            .WithColumn(nameof(AbcMattressPackage.ItemNo)).AsInt32()
            .WithColumn(nameof(AbcMattressPackage.Price)).AsDecimal();
        }
    }
}
