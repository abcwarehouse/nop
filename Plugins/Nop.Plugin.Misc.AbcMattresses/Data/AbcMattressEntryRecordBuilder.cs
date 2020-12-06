using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.AbcMattresses.Domain;

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
            .WithColumn(nameof(AbcMattressEntry.Model)).AsString()
            .WithColumn(nameof(AbcMattressEntry.Size)).AsString()
            .WithColumn(nameof(AbcMattressEntry.MattressItemNo)).AsInt32()
            .WithColumn(nameof(AbcMattressEntry.MattressPrice)).AsDecimal()
            .WithColumn(nameof(AbcMattressEntry.MattressType)).AsString();
        }
    }
}
