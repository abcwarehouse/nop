using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcMattresses.Data
{
    public class AbcMattressBaseRecordBuilder : NopEntityBuilder<AbcMattressBase>
    {
        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
            .WithColumn(nameof(AbcMattressBase.ItemNo)).AsInt32().Unique()
            .WithColumn(nameof(AbcMattressBase.Name)).AsString()            
            .WithColumn(nameof(AbcMattressBase.Price)).AsDecimal()
            .WithColumn(nameof(AbcMattressBase.IsAdjustable)).AsBoolean();
        }
    }
}
