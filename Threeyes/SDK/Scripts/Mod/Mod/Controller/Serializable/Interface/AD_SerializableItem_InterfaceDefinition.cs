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
//——Item——
public interface IAD_SerializableItem :
    IRuntimeEditable,
    IRuntimeSerializableComponent
{
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
    , IContextMenuProvider//由自身提供ContextMenu
{ }
public interface IAD_ShellItem : IAD_SerializableItemWithContextMenu
    , IRuntimeEditorSelectable//运行时可选
{
}
public interface IAD_DecorationItem : IAD_SerializableItemWithContextMenu
    , IRuntimeEditorSelectable//运行时可选
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
/// 存储统一区域内所有预制物信息
/// </summary>
[Serializable]
public class AD_PrefabConfigInfo<TSOPrefabInfoGroup, TSOPrefabInfo>
    where TSOPrefabInfoGroup : AD_SOPrefabInfoGroupBase<TSOPrefabInfo>
    where TSOPrefabInfo : AD_SOPrefabInfo
{
    public bool HasElement
    {
        get
        {
            return listSOPrefabInfoGroup.Count > 0;
        }
    }
    public List<TSOPrefabInfoGroup> ListSOPrefabInfoGroup { get { return listSOPrefabInfoGroup; } set { listSOPrefabInfoGroup = value; } }
    [SerializeField] List<TSOPrefabInfoGroup> listSOPrefabInfoGroup = new List<TSOPrefabInfoGroup>();

    public AD_PrefabConfigInfo()
    {
    }

    public List<TSOPrefabInfo> FindAllPrefabInfo(Predicate<TSOPrefabInfo> match = null)
    {
        List<TSOPrefabInfo> listResult = new List<TSOPrefabInfo>();
        foreach (var soGroup in listSOPrefabInfoGroup)
        {
            if (match != null)
                listResult.AddRange(soGroup.ListData.FindAll(match));
            else
                listResult.AddRange(soGroup.ListData);
        }
        return listResult;
    }
}

public interface IAD_SerializableItemController :
    IElementGroup,
    IModControllerHandler,
    IFilePathModifierHolder
{
    public RuntimeSerializable_GameObject RuntimeSerialization_GameObjectRoot { get; }

    void RelinkElemets();
    void InitExistElements();

    /// <summary>
    /// 使用相同的预制物替换原物体
    /// </summary>
    /// <param name="oldInst"></param>
    /// <param name="prefab"></param>
    /// <param name="actRebind">针对新旧物体的替换回调，第一个参数为旧，第二个参数为新</param>
    /// <returns></returns>
    IAD_SerializableItem ChangeElementStyle(IAD_SerializableItem oldInst, GameObject prefab, UnityAction<GameObject, GameObject> actRebind);
    void DeleteElement(IAD_SerializableItem item);
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