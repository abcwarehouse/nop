namespace AbcWarehouse.Plugin.Widgets.CartSlideout.Models
{
    public class UpdateShoppingCartItemModel
    {
        public int ShoppingCartItemId { get; set; }

        public int ProductAttributeId { get; set; }

        public int ProductAttributeValueId { get; set; }

        public bool? IsChecked { get; set; }

        public bool IsValid()
        {
            return ShoppingCartItemId != 0 &&
                   ProductAttributeId != 0 &&
                   ProductAttributeValueId != 0 &&
                   IsChecked != null;
        }
    }
}