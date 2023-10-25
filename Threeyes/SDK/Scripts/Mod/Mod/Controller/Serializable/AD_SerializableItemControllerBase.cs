using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threeyes.Config;
using Threeyes.Coroutine;
using Threeyes.SpawnPoint;
using UnityEngine;
using Newtonsoft.Json;
using Threeyes.RuntimeSerialization;
using UnityEngine.Events;
using Threeyes.Data;
#if USE_NaughtyAttributes
using NaughtyAttributes;
#endif

/// <summary>
/// PS:
/// -不继承SequenceElementManagerBase，是因为有很多代码不适用
/// </summary>
public abstract class AD_SerializableItemControllerBase<TManager, TSOPrefabInfo, TElement, TEleData, TBaseEleData, TSOConfig, TConfig> : ElementGroupBase<TElement, TEleData>, IConfigurableComponent<TSOConfig, TConfig>
    , IAD_SerializableItemController<TBaseEleData>
    where TManager : AD_SerializableItemControllerBase<TManager, TSOPrefabInfo, TElement, TEleData, TBaseEleData, TSOConfig, TConfig>
    where TSOPrefabInfo : AD_SOPrefabInfoBase
    where TElement : ElementBase<TEleData>, IAD_SerializableItem
    where TEleData : class, IAD_SerializableItemInfo, new()
    where TSOConfig : SOConfigBase<TConfig>
    where TConfig : AD_SerializableItemControllerConfigInfoBase<TSOPrefabInfo>
{
    #region Property & Field

    //Runtime
    public virtual FilePathModifier FilePathModifier { get { return filePathModifier; } set { filePathModifier = value; } }
    private FilePathModifier filePathModifier;


    #region IConfigurableComponent
    public TConfig Config
    {
        get
        {
            if (config == null)
                config = SOOverrideConfig ? SOOverrideConfig.config : DefaultConfig;
            return config;
        }
    }
    protected TConfig config;

    public TConfig DefaultConfig { get { return defaultConfig; } set { defaultConfig = value; } }
    public TSOConfig SOOverrideConfig { get { return soOverrideConfig; } set { soOverrideConfig = value; } }
    [Header("Config")]
    [SerializeField] protected TConfig defaultConfig;//Default config
#if USE_NaughtyAttributes
    [Expandable]
#endif
    [SerializeField] protected TSOConfig soOverrideConfig;//Override config
    #endregion

    public RuntimeSerialization_GameObject RuntimeSerialization_GameObjectRoot { get { return runtimeSerialization_GameObjectRoot; } }
    [SerializeField] private RuntimeSerialization_GameObject runtimeSerialization_GameObjectRoot;

    public Transform tfElementParent;//元素的父物体
    //管理子物体的序列化/反序列化
    public SpawnPointProvider spawnPointProvider;//[Optional]提供生成位置及时间间隔

    #endregion

    #region IAD_SerializableItemController

    public IAD_SerializableItemControllerConfigInfo BaseConfig { get { return Config; } }

    public List<AD_SOPrefabInfoBase> GetAllValidPrefabInfos(IAD_SerializableItemInfo eleData, bool matchingCondition)
    {
        return GetAllValidPrefabInfosFunc(eleData as TEleData, matchingCondition).ConvertAll(pI => pI as AD_SOPrefabInfoBase);
    }
    public AD_SOPrefabInfoBase GetRelatedPrefabInfo(IAD_SerializableItem serializableItem)
    {
        return GetRelatedPrefabInfoFunc(serializableItem);
    }
    public TSOPrefabInfo GetRelatedPrefabInfoFunc(IAD_SerializableItem serializableItem)
    {
        RuntimeSerialization_GameObject runtimeSerialization_GameObject = serializableItem.RuntimeSerialization_GameObject;
        if (!runtimeSerialization_GameObject)
            return null;
        if (!runtimeSerialization_GameObject.cachePrefabMetadata.IsValid)
            return null;

        //从库中查找对应的Prefab，并跟Config中的listPrefab进行匹配
        GameObject prefab = SOAssetPackManager.TryGetPrefab(runtimeSerialization_GameObject.cachePrefabMetadata.Guid);
        return Config.listSOPrefabInfo.FirstOrDefault(soPrefabInfo => soPrefabInfo.prefab == prefab);
    }

    public void DeleteElement(IAD_SerializableItem item)
    {
        DeleteElementFunc(item as TElement);
    }
    protected virtual void DeleteElementFunc(TElement element)
    {
        if (!element)
            return;
        listElement.Remove(element);
        element.gameObject.DestroyAtOnce();
    }
    public IAD_SerializableItem ReCreateElement(IAD_SerializableItem oldInst, GameObject prefab, UnityAction<GameObject, GameObject> actRebind)
    {
        return ReCreateElementFunc(oldInst as TElement, prefab, actRebind);
    }

    /// <summary>
    /// 使用传入的预制物与数据重新创建实例，适用于更换主题（保留位置、旋转、缩放等关系）
    /// </summary>
    /// <param name="oldInst">当前实例</param>
    /// <param name="data"></param>
    /// <param name="prefab"></param>
    protected virtual TElement ReCreateElementFunc(TElement oldInst, GameObject prefab, UnityAction<GameObject, GameObject> actRebind)
    {
        //缓存数据
        Vector3 pos = oldInst.transform.localPosition;
        Quaternion rot = oldInst.transform.localRotation;
        //Vector3 scale = oldInst.transform.localScale;//保留默认缩放（因为每个模型的默认尺寸不一样）


        oldInst.data.IsDestroyRuntimeAssetsOnDispose = false;//禁止旧物体被销毁时Dispose数据，可以保留运行时资源（如Texture）。原因是CopyAllMembersFrom只会拷贝资源的引用而不是克隆。（PS：该字段不会被拷贝）

        //生成实例
        overridePrefab = prefab;//临时更改为目标Prefab
        TElement newInst = InitElement(oldInst.data);//让新元素拷贝旧元素的data并初始化
        AddElementToList(newInst);
        overridePrefab = null; //Reset

        //通知Rebind
        try
        {
            actRebind.Execute(oldInst.gameObject, newInst.gameObject);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Rebind error: " + e);
        }

        //删除旧实例
        listElement.Remove(oldInst);
        oldInst.gameObject.DestroyAtOnce();//PS：会Dispose掉data运行时加载的资源


        //使用旧实例的位置
        newInst.transform.localPosition = pos;
        newInst.transform.localRotation = rot;
        //newInst.transform.localScale = scale;//暂时不使用旧物体的缩放，因为每个Prefab的默认尺寸都有差异
        return newInst;
    }
    #endregion

    #region Init
    public void InitBase(List<TBaseEleData> listBaseEleData, bool isClear)//由子类调用对应的带参构造函数
    {
        if (isClear)
            Clear();
        Init(listBaseEleData.ConvertAll(bD => ConvertFromBaseData(bD)));
    }

    /// <summary>
    /// 将父类封装为子类
    /// 
    /// Warning：需要将IsBaseType设置为true
    /// </summary>
    /// <param name="baseEleData"></param>
    /// <returns></returns>
    protected abstract TEleData ConvertFromBaseData(TBaseEleData baseEleData);

    public virtual void RelinkElemets()
    {
        listElement = tfElementParent.GetComponentsInChildren<TElement>().ToList();
    }
    /// <summary>
    /// 调用现存的元素的Init方法
    /// 用途：
    /// -使用最新数据，进行重新初始化及绑定
    /// </summary>
    public virtual void InitExistElements()
    {
        foreach (var element in listElement)
        {
            InitData(element, element.data);//主要是重新初始化其Manager的信息（后期Data有外部数据时，再更新此实现）
        }
    }

    protected GameObject overridePrefab = null;//Set this to temp override prefab
    protected override TElement CreateElementFunc(TEleData eleData)
    {
        //#1 生成物体
        GameObject prefab = overridePrefab ?? GetFirstValidPrefab(eleData); //从Config中获取首个有效的Prefab
        GameObject goInst = prefab.InstantiatePrefab(tfElementParent);
        TElement element = goInst.GetComponent<TElement>();

        //#2 如果实例有RuntimeSerialization_GameObject，则初始化其PrefabMetadata
        TrySetPrefabMetadata(goInst, prefab);

        return element;
    }

    protected Coroutine cacheEnumInitElementList;
    protected virtual void TryStopCoroutine_InitElementList()
    {
        if (cacheEnumInitElementList != null)
            CoroutineManager.StopCoroutineEx(cacheEnumInitElementList);
    }
    protected virtual void Init(List<TEleData> listData)
    {
        TryStopCoroutine_InitElementList();
        cacheEnumInitElementList = CoroutineManager.StartCoroutineEx(IEInit(listData));
    }
    protected virtual IEnumerator IEInit(List<TEleData> listData)
    {
        foreach (var data in listData)
        {
            ISpawnPointGroup spawnPoint = null;
            if (spawnPointProvider != null)
            {
                spawnPoint = spawnPointProvider.GetNewSpawnPoint();//该行代码可以确保当延时为0时，1帧内完成所有初始化，如果注销则会导致每个物体生成后都会至少等待1帧
                while (spawnPoint == null)
                {
                    spawnPoint = spawnPointProvider.GetNewSpawnPoint();
                    yield return null;//PS：在编辑模式下的FreezeTime会卡住，不影响
                }
            }

            TElement inst = InitElement(data);
            AddElementToList(inst);
            ///如果提供了SpawnPoint，则将其位置/旋转值同步到已生成物体
            if (spawnPoint != null)
            {
                Pose newPose = spawnPoint.spawnPose;
                inst.transform.position = newPose.position;
                inst.transform.rotation = newPose.rotation;
            }
        }
    }

    protected override void InitData(TElement element, TEleData data)
    {
        //#1 初始化相关RuntimeEdit（Warning：需要优先调用，因为后续的InitData会使用到）
        element.InitRuntimeEdit(filePathModifier);

        base.InitData(element, data);

    }
    #endregion

    #region Reset
    public override void ResetElement()
    {
        TryStopCoroutine_InitElementList();
        tfElementParent.DestroyAllChild();
    }
    #endregion

    #region Interfaces

    /// <summary>
    /// 获取匹配该data条件的所有预制物
    /// </summary>
    /// <param name="eleData"></param>
    /// <param name="matchingCondition">是否根据条件进行匹配，如果为否则返回所有预制物。（可通过UI上的一个Toggle开关，方便用户使用其他物体代替）</param>
    /// <returns></returns>
    protected virtual List<TSOPrefabInfo> GetAllValidPrefabInfosFunc(TEleData eleData, bool matchingCondition)
    {
        List<TSOPrefabInfo> listTargetPrefabInfo = new List<TSOPrefabInfo>();

        var listSourcePrefabInfo = Config.listSOPrefabInfo;
        if (matchingCondition)
        {
            GetAllValidPrefabInfos_Matching(eleData, ref listSourcePrefabInfo, ref listTargetPrefabInfo);
        }
        else//不需要匹配：返回所有
        {
            listTargetPrefabInfo.AddRange(listSourcePrefabInfo);
        }
        return listTargetPrefabInfo;
    }
    protected abstract void GetAllValidPrefabInfos_Matching(TEleData eleData, ref List<TSOPrefabInfo> listSourcePrefabInfo, ref List<TSOPrefabInfo> listTargetPrefabInfo);


    /// <summary>
    /// 通过Fallback的方式查找首个有效的Prefab，常用于首次初始化
    /// </summary>
    /// <param name="eleData"></param>
    /// <returns></returns>
    GameObject GetFirstValidPrefab(TEleData eleData)
    {
        //#1 尝试查找匹配
        TSOPrefabInfo targetPrefabInfo = GetAllValidPrefabInfosFunc(eleData, true).FirstOrDefault();

        //#2 如果上述匹配操作返回null，则返回Fallback预制物信息，避免出错
        if (targetPrefabInfo == null)
        {
            Debug.LogError($"Can't find prefabInfo for [{eleData}]! Try get fallback element instead!");//不算错误，仅弹出警告
            targetPrefabInfo = GetFallbackPrefabInfo(eleData);
        }

        //#3 仍然找不到：报错
        if (!targetPrefabInfo)
        {
            Debug.LogError($"Can't find prefadInfo for [{eleData}]! Check if list empty！");
        }
        return targetPrefabInfo?.prefab;
    }

    protected abstract TSOPrefabInfo GetFallbackPrefabInfo(TEleData eleData);
    #endregion

    #region IModControllerHandler
    public virtual void OnModControllerInit()
    {
    }
    public virtual void OnModControllerDeinit()
    {
    }
    #endregion

    #region Utility
    /// <summary>
    /// 如果prefab是SOAssetPack中的一员，且实例含有RuntimeSerialization_GameObject组件，则初始化其PrefabMetadata字段，用于后续反序列化时链接Prefab
    /// </summary>
    /// <param name="goInst">实例</param>
    /// <param name="prefab">所有的预制物</param>
    static void TrySetPrefabMetadata(GameObject goInst, GameObject prefab)
    {
        RuntimeSerialization_GameObject runtimeSerialization_GameObject = goInst.GetComponent<RuntimeSerialization_GameObject>();
        if (runtimeSerialization_GameObject)
        {
            string guid = SOAssetPackManager.GetPrefabMetadata(prefab);
            if (guid.NotNullOrEmpty())
                runtimeSerialization_GameObject.InitPrefabMetadata(guid);
            else
            {
                Debug.LogError($"Can't find prefab guid for [{prefab.name}]!");
            }
        }
    }
    #endregion
}

public interface IAD_SerializableItemControllerConfigInfo
{
    public List<AD_SOPrefabInfoBase> ListSOPrefabInfoBase { get; }
}
public class AD_SerializableItemControllerConfigInfoBase<TSOPrefabInfo> : SerializableDataBase
    , IAD_SerializableItemControllerConfigInfo
    where TSOPrefabInfo : AD_SOPrefabInfoBase
{
    public List<AD_SOPrefabInfoBase> ListSOPrefabInfoBase { get { return listSOPrefabInfo.ConvertAll(pI => pI as AD_SOPrefabInfoBase); } }

    [JsonIgnore] public List<TSOPrefabInfo> listSOPrefabInfo = new List<TSOPrefabInfo>();
}