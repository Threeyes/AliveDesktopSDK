using System.Collections.Generic;
using UnityEngine;
using Threeyes.RuntimeSerialization;
using Threeyes.Steamworks;
using Threeyes.UI;
using UnityEngine.Events;
using Threeyes.RuntimeEditor;
using Threeyes.Data;
using Threeyes.Persistent;
using System;
using Threeyes.Core;
using System.Linq;
//——Item——
public interface IAD_SerializableItem :
    IRuntimeEditable,
    IRuntimeSerializableComponent
{
    bool IsValid { get; }
    IAD_SerializableItemInfo BaseData { get; }
    RuntimeSerializable_GameObject RuntimeSerialization_GameObject { get; set; }
    /// <summary>
    /// 延迟激活特性，此类组件可以默认为disable的状态，如：
    /// -socket等Interactor，如果在初始化时激活会意外吸取其他组件
    /// </summary>
    void DelayActiveFeature(float delayTime);

    /// <summary>
    /// 主动通知更新（用于Simulator）
    /// </summary>
    void UpdateSetting();
}
public interface IAD_SerializableItemWithContextMenu : IAD_SerializableItem
    , IContextMenuProvider//自身能够提供ContextMenu
{ }
public interface IAD_ShellItem : IAD_SerializableItemWithContextMenu
    , IRuntimeEditorSelectable//运行时可选
{
}
public interface IAD_DecorationItem : IAD_SerializableItemWithContextMenu
    , IRuntimeEditorSelectable//运行时可选
    , IRuntimeEditorDeletable//运行时可删除
{
}

//——ItemInfo——
public interface IAD_SerializableItemInfo : IDisposable
{
    bool IsDestroyRuntimeAssetsOnDispose { get; set; }
    bool IsBaseType { get; set; }
    event UnityAction<PersistentChangeState> PersistentChanged { add { } remove { } }

    /// <summary>
    /// 从其他数据类中克隆通用成员（不包括自定义成员）（不包括Action）
    /// 
    /// 实现类：
    /// -通用类（如AD_FileSystemItemInfo）
    /// 
    /// 适用于：
    /// -FileSystemItem在Deserialize后的Restore
    /// </summary>
    /// <param name="otherInst"></param>
    void CopyBaseMembersFrom(object otherInst);

    /// <summary>
    /// 从其他数据类中克隆所有成员（包括自定义成员）（不包括Action）
    /// 
    /// 适用于：
    /// -ReCreateElement：需要同步自定义字段
    /// </summary>
    /// <param name="otherInst"></param>
    void CopyAllMembersFrom(object otherInst);

    /// <summary>
    /// 销毁运行加载的资源
    /// </summary>
    void DestroyRuntimeAssets();
}

//——ItemController——

/// <summary>
/// 保存相同域（如Mod）内的TSOPrefabInfoGroup清单
/// 
/// PS：
/// -用于在Inspector中编辑，以及存储每个Mod对应的信息
/// -可在运行时生成
/// </summary>
[Serializable]
public class AD_PrefabInfoCategoryBase<TSOPrefabInfoGroup, TSOPrefabInfo>
    where TSOPrefabInfoGroup : AD_SOPrefabInfoGroupBase<TSOPrefabInfo>
    where TSOPrefabInfo : AD_SOPrefabInfo
{
    public bool HasElement
    {
        get
        {
            return listData.Count > 0;
        }
    }

    public string title;//Category name
    public List<TSOPrefabInfoGroup> ListData { get { return listData; } set { listData = value; } }
    [SerializeField] List<TSOPrefabInfoGroup> listData = new List<TSOPrefabInfoGroup>();

    public AD_PrefabInfoCategoryBase() { }

    public AD_PrefabInfoCategoryBase(string title, List<TSOPrefabInfoGroup> listSOPrefabInfoGroup)
    {
        this.title = title;
        this.listData = listSOPrefabInfoGroup;
    }

    public List<TSOPrefabInfo> FindAllPrefabInfo(Predicate<TSOPrefabInfo> match = null)
    {
        List<TSOPrefabInfo> listResult = new List<TSOPrefabInfo>();
        foreach (var soGroup in listData)
        {
            if (match != null)
                listResult.AddRange(soGroup.ListData.FindAll(match));
            else
                listResult.AddRange(soGroup.ListData);
        }
        return listResult;
    }
}

/// <summary>
/// 存储内所有预制物信息
/// 
/// #结构：
/// Category
///     -Group (SOPrefabInfoGroup)
///         -Element (SOPrefabInfo)
/// </summary>
[Serializable]
public class AD_PrefabConfigInfo<TPrefabInfoCategory, TSOPrefabInfoGroup, TSOPrefabInfo>
    where TPrefabInfoCategory : AD_PrefabInfoCategoryBase<TSOPrefabInfoGroup, TSOPrefabInfo>
    where TSOPrefabInfoGroup : AD_SOPrefabInfoGroupBase<TSOPrefabInfo>
    where TSOPrefabInfo : AD_SOPrefabInfo
{
    public bool HasElement
    {
        get
        {
            return listPrefabInfoCategory.Any(c => c.HasElement);
        }
    }

    public List<TPrefabInfoCategory> ListPrefabInfoCategory { get { return listPrefabInfoCategory; } set { listPrefabInfoCategory = value; } }
    [SerializeField] List<TPrefabInfoCategory> listPrefabInfoCategory = new List<TPrefabInfoCategory>();

    public AD_PrefabConfigInfo() { }

    public AD_PrefabConfigInfo(List<TPrefabInfoCategory> listPrefabInfoCategory)
    {
        this.listPrefabInfoCategory = listPrefabInfoCategory;
    }

    public List<TSOPrefabInfo> FindAllPrefabInfo(Predicate<TSOPrefabInfo> match = null)
    {
        List<TSOPrefabInfo> listResult = new List<TSOPrefabInfo>();
        foreach (var catelogy in listPrefabInfoCategory)
        {
            listResult.AddRange(catelogy.FindAllPrefabInfo(match));
        }
        return listResult;
    }
}

public interface IAD_SerializableItemController :
    IElementGroup,
    IModControllerHandler,
    IFilePathModifierHolder
{
    Transform TfElementParent { get; }
    RuntimeSerializable_GameObject RuntimeSerialization_GameObjectRoot { get; }

    void RelinkElemets();
    void InitExistElements();

    /// <summary>
    /// 使用相同的预制物替换原物体
    /// </summary>
    /// <param name="oldInst"></param>
    /// <param name="prefab"></param>
    /// <param name="actRebind">针对新旧物体的替换回调，第一个参数为旧，第二个参数为新</param>
    /// <returns></returns>
    IAD_SerializableItem ChangeElement(IAD_SerializableItem oldInst, GameObject prefab, UnityAction<GameObject, GameObject> actRebind);
}

public interface IAD_SerializableItemController<TBaseEleData> : IAD_SerializableItemController
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="listBaseEleData"></param>
    /// <param name="isClear">true:清空; false:叠加</param>
    void InitBase(List<TBaseEleData> listBaseEleData, bool isClear);

    /// <summary>
    /// 是否包含与给定SOAssetPack相关的实例
    /// </summary>
    /// <param name="sOAssetPack"></param>
    /// <returns></returns>
    bool HasAnyRelatedInstance(SOAssetPack sOAssetPack);
}