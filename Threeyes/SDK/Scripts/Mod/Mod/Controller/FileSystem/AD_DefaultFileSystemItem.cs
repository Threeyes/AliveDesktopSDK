using Newtonsoft.Json;
using Threeyes.Persistent;
using Threeyes.RuntimeEditor;
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
public sealed class AD_DefaultFileSystemItem : AD_SerializableItemWithContextMenuBase<AD_DefaultFileSystemItem, AD_DefaultFileSystemItem.ItemInfo, AD_DefaultFileSystemItem.ItemPropertyBag>
    , IAD_FileSystemItem
    //, IPointerClickHandler
{

    //ToUpdate:参考XRInteractableAffordanceStateProvider/Receiver，进行功能的分离（主要是剥离出各种初始化/更新/重置UnityEvent），方便用户自定义呈现方式
    public StringEvent onInitName;
    public TextureEvent onInitPreview;

    protected override void UpdateSetting()
    {
        //ToUpdate：根据全局设置，决定是否带后缀
        string displayName = data.overrideName.NotNullOrEmpty() ? data.overrideName : data.NameWithoutExtension;
        onInitName.Invoke(displayName);

        Texture preview = data.externalPreview ?? data.TexturePreview;
        onInitPreview.Invoke(preview);
    }


    /////ToDelete：
    /////-将Open改为该类的一个公开方法，并且是调用Manager接口（可以在设置中设定Modder能否提供该能力）。
    /////-用户除了右键打开外，还能通过额外有意思的方式打开。（如触碰、射击等）
    /////-参考XRSimpleInteractable，可以封装其EventData，方便后续实现。或将该OnPointerClick方法提炼到通用类AD_XXBehaviour中，参考AC_InputBehaviour，增加类似OnClick、OnDoubleClick等事件。
    //public void OnPointerClick(PointerEventData eventData)
    //{
    //    if (IsRuntimeEditorMode)
    //        return;
    //    //PS：建议参考系统的实现，用双击代替，因为拖拽时抬起也算单击
    //    if (eventData.clickCount == 2)
    //    {
    //        //UnityEngine.Debug.LogError("Double Click!");
    //        AD_FileSystemManager.OpenItem(data);
    //    }
    //    //else if (eventData.button == PointerEventData.InputButton.Right)//有效，但由IContextMenuHolder管理
    //    //{
    //    //    UnityEngine.Debug.LogError("Right Click!");
    //    //}
    //}

    #region Test ContextMenuInfo [ToDelete]
    //public override List<ToolStripItemInfo> GetContextMenuInfo()
    //{
    //    if (IsRuntimeEditorMode)
    //        return new List<ToolStripItemInfo>()
    //    {
    //        //ToUpdate:改为在Inspector中显示
    //    new ToolStripMenuItemInfo("Modify Data", (o, arg) =>OpenUIObjectModifierManager())
    //    };
    //    return base.GetContextMenuInfo();
    //}
    //void OpenUIObjectModifierManager()
    //{
    //    var targetValue = data;
    //    var originClone = UnityObjectTool.DeepCopy(targetValue);
    //    var originBackup = UnityObjectTool.DeepCopy(targetValue);

    //    UIObjectModifierManager.Instance.Create(UIRuntimeEditorFactory.Instance, originClone, originBackup, FilePathModifier, targetValue.GetType().ToString(),
    //            (result) =>
    //            {
    //                //模拟PersistentDataComplexBase.OnValueChanged的数据更新
    //                PersistentObjectTool.CopyFiledsAndLoadAsset(data, result, PersistentChangeState.Set, FilePathModifier.ParentDir);
    //            });
    //}

    #endregion

    #region Define
    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class ItemInfo : AD_FileSystemItemInfo
    {
        /// Todo:增加（读取或Refresh时这些字段不要覆盖，而是仅复制父类公有的部分，保存用户自定义字段）（编辑器选中后可在Inspector设置，最终会通过RTS保存。反序列化后可以在Init方法中读取对应配置并初始化）：
        ///     -nameOverride
        ///     -previewPathOverride及对应的路径：支持用户自定义单个文件的预览图
        /// -使用Attribute标记可编辑的用户自定义字段（参考Json，前缀为RuntimeEdit）
        /// -多语言（只需要提供Key，然后通过Key查询到对应的多语言值）
        [RuntimeEditorProperty] public string overrideName;

        [JsonIgnore] public Texture externalPreview;
        [RuntimeEditorProperty] [PersistentAssetFilePath(nameof(externalPreview), true, defaultAssetFieldName: nameof(texturePreview))] public string overridePreviewFilePath;

        [HideInInspector] [JsonIgnore] [PersistentDirPath] public string PersistentDirPath;

        public ItemInfo()
        {
        }
        public ItemInfo(AD_FileSystemItemInfo otherInst) : base(otherInst)
        {
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
        }

        public override void DestroyRuntimeAssets()
        {
            base.DestroyRuntimeAssets();
            if (externalPreview)
                UnityEngine.Object.Destroy(externalPreview);
        }
    }

    public class ItemPropertyBag : AD_SerializableItemPropertyBagBase<AD_DefaultFileSystemItem, ItemInfo>
    {
        public ItemPropertyBag()
        {
        }
    }
    #endregion
}