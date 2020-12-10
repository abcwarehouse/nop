using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Data.Extensions;
using System.Data;

namespace Nop.Plugin.Misc.AbcMattresses.Data
{
    public class AbcMattressEntryRecordBuilder : NopEntityBuilder<AbcMattressEntry>
    {
        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
            .WithColumn(nameof(AbcMattressEntry.AbcMattressModelId)).AsInt32().ForeignKey<AbcMattressModel>(onDelete: Rule.Cascade)
            .WithColumn(nameof(AbcMattressEntry.Size)).AsString()
            .WithColumn(nameof(AbcMattressEntry.ItemNo)).AsInt32()
            .WithColumn(nameof(AbcMattressEntry.Price)).AsDecimal();
        }
    }
}
