using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcMattresses.Data
{
    public class AbcMattressRecordBuilder : NopEntityBuilder<AbcMattress>
    {
        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
            .WithColumn(nameof(AbcMattress.Model)).AsString().Unique()
            .WithColumn(nameof(AbcMattress.Brand)).AsString()
            .WithColumn(nameof(AbcMattress.Comfort)).AsString();
        }
    }
}
