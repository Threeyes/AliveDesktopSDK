using Threeyes.Config;
using Threeyes.Data;
public sealed class AD_DefaultDecorationItem : AD_SerializableItemWithContextMenuBase<AD_DefaultDecorationItem, AD_DefaultDecorationItem.ItemInfo, AD_DefaultDecorationItem.ItemPropertyBag>
    , IAD_DecorationItem
{
    protected override void UpdateSetting()
    {
        //ToAdd
    }
    #region Define
    /// <summary>
    /// 与该Item绑定的数据
    /// </summary>
    [System.Serializable]
    public class ItemInfo : AD_DecorationItemInfo
    {
        public ItemInfo()
        {
        }

        public ItemInfo(AD_DecorationItemInfo otherInst) : base(otherInst)
        {
        }
    }
    public class ItemPropertyBag : AD_SerializableItemPropertyBagBase<AD_DefaultDecorationItem, ItemInfo>
    {
        public ItemPropertyBag()
        {
        }
    }
    #endregion
}
