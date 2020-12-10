using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Data.Extensions;
using System.Data;

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
            .WithColumn(nameof(AbcMattressPackage.AbcMattressEntryId)).AsInt32().ForeignKey<AbcMattressEntry>(onDelete: Rule.Cascade)
            .WithColumn(nameof(AbcMattressPackage.AbcMattressBaseId)).AsInt32().ForeignKey<AbcMattressBase>(onDelete: Rule.Cascade)
            .WithColumn(nameof(AbcMattressPackage.ItemNo)).AsInt32()
            .WithColumn(nameof(AbcMattressPackage.Price)).AsDecimal();
        }
    }
}
