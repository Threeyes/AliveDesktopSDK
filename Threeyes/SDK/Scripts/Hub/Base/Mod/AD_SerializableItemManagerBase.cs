using System.Collections.Generic;
using System.Linq;
using Threeyes.Core;
using Threeyes.Steamworks;
using UnityEngine.SceneManagement;
public interface IAD_SerializableItemManager<TPrefabConfigInfo, TPrefabInfoCategory, TSOPrefabInfoGroup, TSOPrefabInfo>
    where TPrefabConfigInfo : AD_PrefabConfigInfo<TPrefabInfoCategory, TSOPrefabInfoGroup, TSOPrefabInfo>, new()
    where TPrefabInfoCategory : AD_PrefabInfoCategoryBase<TSOPrefabInfoGroup, TSOPrefabInfo>
    where TSOPrefabInfoGroup : AD_SOPrefabInfoGroupBase<TSOPrefabInfo>
    where TSOPrefabInfo : AD_SOPrefabInfo
{
    TPrefabConfigInfo SdkPrefabConfigInfo { get; }

    List<TPrefabConfigInfo> GetAllPrefabConfigInfo();
}

/// <summary>
/// 带序列化物体组件的Manager
/// 
/// ToUpdate:
/// -拆分出SDK相关：
///     -把IContextMenuResolver、IsRuntimeEditorMode、AD_RuntimeSerializationController等挪到AD_HubSerializableItemManagerBase子基类中
///     -
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TControllerInterface"></typeparam>
/// <typeparam name="TDefaultController"></typeparam>
/// <typeparam name="TBaseEleData"></typeparam>
public abstract class AD_SerializableItemManagerBase<T, TControllerInterface, TDefaultController, TPrefabConfigInfo, TPrefabInfoCategory, TSOPrefabInfoGroup, TSOPrefabInfo, TBaseEleData> : HubManagerWithControllerBase<T, TControllerInterface, TDefaultController>
    , IAD_SerializableItemManager<TPrefabConfigInfo, TPrefabInfoCategory, TSOPrefabInfoGroup, TSOPrefabInfo>
    , IHubManagerModPreInitHandler
    , IHubManagerModInitHandler
    where T : HubManagerWithControllerBase<T, TControllerInterface, TDefaultController>
    where TControllerInterface : class, IAD_SerializableItemController<TBaseEleData>
    where TDefaultController : TControllerInterface
    where TPrefabConfigInfo : AD_PrefabConfigInfo<TPrefabInfoCategory, TSOPrefabInfoGroup, TSOPrefabInfo>, new()
    where TPrefabInfoCategory : AD_PrefabInfoCategoryBase<TSOPrefabInfoGroup, TSOPrefabInfo>
    where TSOPrefabInfoGroup : AD_SOPrefabInfoGroupBase<TSOPrefabInfo>
    where TSOPrefabInfo : AD_SOPrefabInfo
    where TBaseEleData : class, IAD_SerializableItemInfo
{
    public TPrefabConfigInfo SdkPrefabConfigInfo { get { return sdkPrefabConfigInfo; } }

    [UnityEngine.SerializeField] protected TPrefabConfigInfo sdkPrefabConfigInfo = new TPrefabConfigInfo();

    public abstract List<TPrefabConfigInfo> GetAllPrefabConfigInfo();

    #region IHubManagerModPreInitHandler
    public virtual void OnModPreInit(Scene scene, ModEntry modEntry)
    {
        //由子类实现
    }
    #endregion

    #region IHubManagerModInitHandler
    public virtual void OnModInit(Scene scene, ModEntry modEntry)
    {
        //由子类实现
    }
    public virtual void OnModDeinit(Scene scene, ModEntry modEntry)
    {
        modController?.OnModControllerDeinit();
        modController = null;//重置，否则会有引用残留
    }

    protected abstract void InitWithDefaultDatas();
    #endregion
}
