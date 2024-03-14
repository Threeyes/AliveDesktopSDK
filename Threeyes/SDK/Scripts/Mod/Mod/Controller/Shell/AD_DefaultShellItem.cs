using Newtonsoft.Json;
using Threeyes.Core;
using Threeyes.Persistent;
using Threeyes.RuntimeEditor;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// 
/// Todo：
/// -提供默认的双击打开、右键菜单的交互，Modder可以override对应方法
/// -可以是用户
/// +改名为Default或其他前缀，方便确认
/// -Data增加一个overrideDisplayName，方便用户更改其显示名称（也可以是直接为源文件改名并重链接）
/// </summary>
public sealed class AD_DefaultShellItem : AD_SerializableItemWithContextMenuBase<AD_DefaultShellItem, AD_DefaultShellItem.ItemInfo, AD_DefaultShellItem.ItemPropertyBag>
    , IAD_FileSystemItem
{
    public StringEvent onInitName;
    public TextureEvent onInitPreview;
    public override void UpdateSetting()
    {
        string displayName = data.NameWithoutExtension; //ToUpdate：根据全局设置，决定是否带后缀
        if (data.overrideName.NotNullOrEmpty())
            displayName = data.overrideName;
        onInitName.Invoke(displayName);

        Texture preview = data.externalPreview;
        if (preview == null)
            preview = data.TexturePreview;
        onInitPreview.Invoke(preview);
    }

    #region IRuntimeEditable
    public override string RuntimeEditableDisplayName
    {
        get
        {
            return data.DisplayName;
        }
    }
    #endregion

    #region Define
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class ItemInfo : AD_ShellItemInfo
    {
        ///20231114注意：
        ///
        ///-既可以通过Event更新信息，也可以通过Controller进行进一步调整

        /// Todo:
        /// +增加特殊字段（读取或Refresh时这些字段不要覆盖，而是仅复制父类公有的部分，保存用户自定义字段）（编辑器选中后可在Inspector设置，最终会通过RTS保存。反序列化后可以在Init方法中读取对应配置并初始化）
        ///     +nameOverride
        ///     +previewPathOverride及对应的路径：支持用户自定义单个文件的预览图
        /// +将name标记为RuntimeReadOnly，方便用户知道其默认名称。
        /// +使用Attribute标记可编辑的用户自定义字段（参考Json，前缀为RuntimeEdit）
        /// -多语言（只需要提供Key，然后通过Key查询到对应的多语言值）
        [RuntimeEditorProperty] public string overrideName;

        [JsonIgnore] public Texture externalPreview;
        [RuntimeEditorProperty] [PersistentAssetFilePath(nameof(externalPreview), true, defaultAssetFieldName: nameof(texturePreview))] public string overridePreviewFilePath;//支持用户自定义文件的预览图

        [HideInInspector] [JsonIgnore] [PersistentDirPath] public string PersistentDirPath;

        public ItemInfo()
        {
        }
        public ItemInfo(AD_ShellItemInfo otherInst) : base(otherInst)
        {
        }

        public override void ResetUserCustomData()
        {
            base.ResetUserCustomData();
            //清空用户自定义数据（其实就是本子类中重载的所有字段）
            overrideName = "";
            overridePreviewFilePath = "";
        }

        public override void CopyAllMembersFrom(object otherInst)
        {
            base.CopyAllMembersFrom(otherInst);

            if (otherInst is ItemInfo otherItemInfo)
            {
                overrideName = otherItemInfo.overrideName;
                externalPreview = otherItemInfo.externalPreview;
                overridePreviewFilePath = otherItemInfo.overridePreviewFilePath;
                PersistentDirPath = otherItemInfo.PersistentDirPath;
            }

            ///ToAdd:
            ///-- 通过反序列化/序列化接口，将引用的XXContorller拷贝到新实例上（可以在ItemController中实现）
        }

        public override void DestroyRuntimeAssets()
        {
            base.DestroyRuntimeAssets();
            if (externalPreview)
                Destroy(externalPreview);
        }
    }

    public class ItemPropertyBag : AD_SerializableItemPropertyBagBase<AD_DefaultShellItem, ItemInfo>
    {
        public ItemPropertyBag()
        {
        }
    }
    #endregion
}