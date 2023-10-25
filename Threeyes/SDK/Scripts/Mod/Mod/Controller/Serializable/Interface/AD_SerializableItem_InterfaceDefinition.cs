using System.Collections.Generic;
using UnityEngine;
using Threeyes.RuntimeSerialization;
using Threeyes.Steamworks;
using Threeyes.UI;
using UnityEngine.Events;
using Threeyes.RuntimeEditor;
using Threeyes.Data;
using Threeyes.Persistent;

//——Item——
public interface IAD_SerializableItem : IRuntimeEditable
{
    IAD_SerializableItemInfo BaseData { get; }
    RuntimeSerialization_GameObject RuntimeSerialization_GameObject { get; }

    void InitRuntimeEdit(FilePathModifier filePathModifier);
}
public interface IAD_SerializableItemWithContextMenu : IAD_SerializableItem
    //, IContextMenuTrigger//由Manager提供ContextMenu（如敏感方法）（PS:IContextMenuProvider已经继承该方法）
    , IContextMenuProvider//由自身提供ContextMenu
{ }
public interface IAD_FileSystemItem : IAD_SerializableItemWithContextMenu
{
}
public interface IAD_DecorationItem : IAD_SerializableItemWithContextMenu
{
}

//——ItemInfo——
public interface IAD_SerializableItemInfo : System.IDisposable
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

public interface IAD_SerializableItemController :
    IElementGroup,
    IModControllerHandler,
    IFilePathModifierHolder
{
    public RuntimeSerialization_GameObject RuntimeSerialization_GameObjectRoot { get; }
    IAD_SerializableItemControllerConfigInfo BaseConfig { get; }

    void RelinkElemets();
    void InitExistElements();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="oldInst"></param>
    /// <param name="prefab"></param>
    /// <param name="actRebind">针对新旧物体的替换回调，第一个参数为旧，第二个参数为新</param>
    /// <returns></returns>
    IAD_SerializableItem ReCreateElement(IAD_SerializableItem oldInst, GameObject prefab, UnityAction<GameObject, GameObject> actRebind);
    void DeleteElement(IAD_SerializableItem item);

    /// <summary>
    /// 获取所有可用的PrefaibInfo
    /// </summary>
    /// <param name="eleData"></param>
    /// <param name="matchingCondition"></param>
    /// <returns></returns>
    List<AD_SOPrefabInfoBase> GetAllValidPrefabInfos(IAD_SerializableItemInfo eleData, bool matchingCondition);

    /// <summary>
    /// 尝试查找实例所使用的PrefaibInfo
    /// </summary>
    /// <param name="runtimeSerialization_GameObject"></param>
    /// <returns>如果rts_GO为空或者无法找到，就返回null</returns>
    AD_SOPrefabInfoBase GetRelatedPrefabInfo(IAD_SerializableItem item);
}

public interface IAD_SerializableItemController<TBaseEleData> : IAD_SerializableItemController
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="listBaseEleData"></param>
    /// <param name="isClear">true:清空; false:叠加</param>
    void InitBase(List<TBaseEleData> listBaseEleData, bool isClear);
}