using System.Data;
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Catalog;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Data.Extensions;

namespace Nop.Plugin.Misc.AbcMattresses.Data
{
    public class AbcMattressModelRecordBuilder : NopEntityBuilder<AbcMattressModel>
    {
        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
            .WithColumn(nameof(AbcMattressModel.Name)).AsString()
            .WithColumn(nameof(AbcMattressModel.ManufacturerId))
                .AsInt32()
                .Nullable()
                .ForeignKey<Manufacturer>(onDelete: Rule.SetNull)
            .WithColumn(nameof(AbcMattressModel.Description)).AsString()
            .WithColumn(nameof(AbcMattressModel.Comfort)).AsString()
            .WithColumn(nameof(AbcMattressModel.ProductId))
                .AsInt32()
                .Nullable()
                .ForeignKey<Product>(onDelete: Rule.SetNull);
        }
    }
}
