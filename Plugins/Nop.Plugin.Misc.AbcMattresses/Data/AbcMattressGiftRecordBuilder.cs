using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcMattresses.Data
{
    public class AbcMattressGiftRecordBuilder : NopEntityBuilder<AbcMattressGift>
    {
        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
            .WithColumn(nameof(AbcMattressGift.Model)).AsString()
            .WithColumn(nameof(AbcMattressGift.ItemNo)).AsInt32()
            .WithColumn(nameof(AbcMattressGift.Description)).AsString()
            .WithColumn(nameof(AbcMattressGift.Amount)).AsDecimal();
        }
    }
}
