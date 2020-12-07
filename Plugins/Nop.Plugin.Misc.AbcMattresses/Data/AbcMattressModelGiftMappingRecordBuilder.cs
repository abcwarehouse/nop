using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Data.Extensions;
using System.Data;

namespace Nop.Plugin.Misc.AbcMattresses.Data
{
    public class AbcMattressModelGiftMappingRecordBuilder : NopEntityBuilder<AbcMattressBase>
    {
        /// <summary>
        /// Apply entity configuration
        /// </summary>
        /// <param name="table">Create table expression builder</param>
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
            .WithColumn(nameof(AbcMattressModelGiftMapping.AbcMattressModelId))
                .AsInt32()
                .ForeignKey<AbcMattressModel>(onDelete: Rule.Cascade)
            .WithColumn(nameof(AbcMattressModelGiftMapping.AbcMattressGiftId))
                .AsInt32()
                .ForeignKey<AbcMattressGift>(onDelete: Rule.Cascade);
        }
    }
}
