using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threeyes.Config;
using Threeyes.SpawnPoint;
using UnityEngine;
using Threeyes.RuntimeSerialization;
using UnityEngine.Events;
using Threeyes.Data;
using Threeyes.RuntimeEditor;
using Threeyes.Core;
using Threeyes.GameFramework;
#if USE_NaughtyAttributes
using NaughtyAttributes;
#endif

/// <summary>
/// PS:
/// -不继承SequenceElementManagerBase，是因为有很多代码不适用
/// </summary>
public abstract class AD_SerializableItemControllerBase<TManager, TSOPrefabInfoGroup, TSOPrefabInfo, TElement, TEleData, TBaseEleData, TSOConfig, TConfig> : ElementGroupBase<TElement, TEleData>,
    IAD_SerializableItemController<TBaseEleData>,
    IConfigurableComponent<TSOConfig, TConfig>
    where TManager : AD_SerializableItemControllerBase<TManager, TSOPrefabInfoGroup, TSOPrefabInfo, TElement, TEleData, TBaseEleData, TSOConfig, TConfig>
    where TSOPrefabInfoGroup : AD_SOPrefabInfoGroupBase<TSOPrefabInfo>
    where TSOPrefabInfo : AD_SOPrefabInfo
    where TElement : ElementBase<TEleData>, IAD_SerializableItem
    where TEleData : class, IAD_SerializableItemInfo, new()
    where TSOConfig : SOConfigBase<TConfig>
    where TConfig : AD_SerializableItemControllerConfigInfoBase<TSOPrefabInfo>, new()
{
    #region Property & Field

    public Transform TfElementParent { get { return tfElementParent; } }
    public Transform tfElementParent;//元素的父物体
    //管理子物体的序列化/反序列化
    public SpawnPointProvider spawnPointProvider;//[Optional]提供生成位置及时间间隔

    //Runtime
    public FilePathModifier FilePathModifier { get { return filePathModifier; } set { filePathModifier = value; } }
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

    public RuntimeSerializable_GameObject RuntimeSerialization_GameObjectRoot { get { return runtimeSerialization_GameObjectRoot; } }
    [SerializeField] RuntimeSerializable_GameObject runtimeSerialization_GameObjectRoot;
    #endregion

    #region IAD_SerializableItemController
    public IAD_SerializableItem ChangeElement(IAD_SerializableItem oldInst, GameObject prefab, UnityAction<GameObject, GameObject> actRebind)
    {
        return ChangeElementFunc(oldInst as TElement, prefab, actRebind);
    }

    /// <summary>
    /// 使用传入的预制物与数据重新创建实例，适用于更换主题（保留位置、旋转等属性）
    /// </summary>
    /// <param name="oldInst">当前实例</param>
    /// <param name="data"></param>
    /// <param name="prefab"></param>
    protected virtual TElement ChangeElementFunc(TElement oldInst, GameObject prefab, UnityAction<GameObject, GameObject> actRebind)
    {
        //缓存数据
        Vector3 pos = oldInst.transform.position;
        Quaternion rot = oldInst.transform.rotation;
        //Vector3 scale = oldInst.transform.localScale;//保留新示例的+默认缩放（因为每个模型的默认尺寸不一样）

        //生成实例
        oldInst.data.IsDestroyRuntimeAssetsOnDispose = false;//标记禁止旧物体被销毁时Dispose数据，以便保留Manager外部加载的资源（如texturePreview），后续通过CopyAllMembersFrom复制字段
        overridePrefab = prefab;//临时更改为目标Prefab
        TElement newInst = InitElement(oldInst.data);//让新元素拷贝旧元素的data并初始化
        actRebind.TryExecute(oldInst.gameObject, newInst.gameObject);//通知重新绑定(如更新选择)
  
        //# Init
        newInst.transform.SetProperty(pos, rot, isLocalSpace: false);//使用旧实例的位置
        AddElementToList(newInst);

        //# Reset
        listElement.Remove(oldInst);//删除旧实例
        oldInst.gameObject.DestroyAtOnce();//PS：会Dispose掉data运行时加载的资源
        overridePrefab = null;

        return newInst;
    }

    public bool HasAnyRelatedInstance(SOAssetPack sOAssetPack)
    {
        if (!sOAssetPack)
            return false;

        foreach (var ele in listElement)
        {
            RuntimeSerializable_GameObject runtimeSerialization_GameObject = ele.GetComponent<RuntimeSerializable_GameObject>();
            if (runtimeSerialization_GameObject)
            {
                bool hasRelatedPrefab = sOAssetPack.TryGetPrefab(runtimeSerialization_GameObject.CachePrefabID.Guid) != null;
                if (hasRelatedPrefab)
                    return true;
            }
        }
        return false;
    }
    #endregion

    #region Init
    /// <summary>
    /// 通过PD恢复子物体后，重建链接
    /// </summary>
    public virtual void RelinkElemets()
    {
        listElement = tfElementParent.GetComponentsInChildren<TElement>(true).ToList();//要包括隐藏的物体
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
            InitData(element, element.data);//主要是重新初始化对应Manager的信息（后期Data有外部数据时，再更新此实现）
        }
    }

    public void InitBase(List<TBaseEleData> listBaseEleData, bool isClear)
    {
        if (isClear)
            Clear();
        InitWithData(listBaseEleData.ConvertAll(bD => ConvertFromBaseData(bD)));//调用对应的带参构造函数
    }

    /// <summary>
    /// 将父类封装为子类
    /// 
    /// Warning：需要将IsBaseType设置为true
    /// 
    /// ToUpdate:
    /// -Shell和Decoration要有不同实现（Shell要GetFirstValidPrefab；Decoration要overridePrefab）
    /// </summary>
    /// <param name="baseEleData"></param>
    /// <returns></returns>
    protected abstract TEleData ConvertFromBaseData(TBaseEleData baseEleData);

    protected override TElement CreateElementFunc(TEleData eleData)
    {
        //#1 生成物体
        GameObject prefab = GetPrefab(eleData); //从Config中获取首个有效的Prefab
        GameObject goInst = prefab.InstantiatePrefab(tfElementParent);
        //goInst.name = prefab.name;//使用Prefab的名称，避免显示(Clone)
        TElement element = goInst.GetComponent<TElement>();

        //#2 如果实例有RuntimeSerialization_GameObject，则初始化其PrefabMetadata（模拟RSGO反序列化的流程，方便实时生成的物体能正常被序列化）
        if (!GameFrameworkTool.IsSimulator)
        {
            TryInitPrefabMetadata(goInst, prefab);
        }
        return element;
    }

    protected GameObject overridePrefab = null;//Set this to temp override prefab
    /// <summary>
    /// 获取创建数据所需的Prefab
    /// </summary>
    /// <param name="eleData"></param>
    /// <returns></returns>
    protected virtual GameObject GetPrefab(TEleData eleData)
    {
        return overridePrefab;
    }

    protected Coroutine cacheEnumInitElementList;
    protected virtual void TryStopCoroutine_InitElementList()
    {
        if (cacheEnumInitElementList != null)
            CoroutineManager.StopCoroutineEx(cacheEnumInitElementList);
    }
    protected virtual void InitWithData(List<TEleData> listData)
    {
        TryStopCoroutine_InitElementList();
        cacheEnumInitElementList = CoroutineManager.StartCoroutineEx(IEInit(listData));
    }
    protected virtual IEnumerator IEInit(List<TEleData> listData)
    {
        foreach (var data in listData)
        {
            ISpawnPointGroup spawnPointGroup = null;
            if (spawnPointProvider != null)
            {
                spawnPointGroup = spawnPointProvider.GetNewSpawnPointGroup();//该行代码可以确保当延时为0时，1帧内完成所有初始化，如果注销则会导致每个物体生成后都会至少等待1帧
                while (spawnPointGroup == null)
                {
                    spawnPointGroup = spawnPointProvider.GetNewSpawnPointGroup();//等待下一有效生成点
                    yield return null;//PS：在编辑模式下的FreezeTime会卡住，不影响
                }
            }

            TElement inst = InitElement(data);
            inst.DelayActiveFeature(1);//延后激活特性
            AddElementToList(inst);
            ///如果提供了SpawnPoint，则将其位置/旋转值同步到已生成物体
            if (spawnPointGroup != null)
            {
                Pose newPose = spawnPointGroup.spawnPose;
                inst.transform.SetProperty(newPose.position, newPose.rotation, isLocalSpace: false);
                //inst.transform.position = newPose.position;
                //inst.transform.rotation = newPose.rotation;
            }
        }
    }

    protected override void InitData(TElement element, TEleData data)
    {
        //#1 初始化Element的数据
        base.InitData(element, data);

        //#2 【非编辑模式】初始化该物体所有继承IRuntimeEditable接口的组件（包括该Element自身），会同时加载数据的对应文件及通知事件
        if (!GameFrameworkTool.IsSimulator)
        {
            List<IRuntimeEditable> listRuntimeEditable = element.GetComponents<IRuntimeEditable>().ToList();
            listRuntimeEditable.Remove(element);
            listRuntimeEditable.Insert(0, element);//将Item放在首位，优先初始化
            listRuntimeEditable.ForEach(sI => sI.InitRuntimeEditable(filePathModifier));//模拟物体的初始化调用
        }
        else//【Simulator】
        {
            element.UpdateSetting();//手动调用其更新方法，使用初始数据进行初始化
        }
    }
    #endregion

    #region Reset
    public override void ResetElement()
    {
        TryStopCoroutine_InitElementList();
        tfElementParent.DestroyAllChild();
    }
    #endregion

    #region IModControllerHandler
    public virtual void OnModControllerInit()
    {
    }
    public virtual void OnModControllerDeinit()
    {
        TryStopCoroutine_InitElementList();//停止创建新物体
    }
    #endregion

    #region Utility
    /// <summary>
    /// 如果prefab是SOAssetPack中的一员，且实例含有RuntimeSerialization_GameObject组件，则初始化其PrefabMetadata字段，用于后续反序列化时重链接Prefab。
    /// 还有一种用途是设置运行时所属的Scope
    /// 
    /// PS：
    /// -因为prefab物体是通过AD_SOPrefabInfoGroupBase引用，所以要在SOAssetPackManager中查询其对应的信息
    /// </summary>
    /// <param name="goInst">实例</param>
    /// <param name="prefab">所有的预制物</param>
    static void TryInitPrefabMetadata(GameObject goInst, GameObject prefab)
    {
        RuntimeSerializable_GameObject runtimeSerialization_GameObject = goInst.GetComponent<RuntimeSerializable_GameObject>();
        if (runtimeSerialization_GameObject)
        {
            string guid, scope;
            if (SOAssetPackManager.GetPrefabMetadata(prefab, out guid, out scope))
                runtimeSerialization_GameObject.InitPrefabMetadata(guid, scope);
            else
            {
                Debug.LogError($"Can't find prefab guid for [{prefab.name}]! Check if the prefab exists in all alive SOAssetPacks!");
            }
        }
    }
    #endregion
}

public class AD_SerializableItemControllerConfigInfoBase<TSOPrefabInfo> : SerializableDataBase
    where TSOPrefabInfo : AD_SOPrefabInfo
{
}