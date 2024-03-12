using System.Collections;
using Threeyes.Coroutine;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// 功能：
/// -所有AD的可交互组件应使用或参考该组件的实现
/// -保证抓取时不会更改物体的层级，常用于FileSystem/DecorationItem等需要进行层级序列化的物体
/// 
/// Todo：
/// -暴露可配置内容，如attachTransform的位置/旋转，方便用于自定义附着点的偏移及朝向等
/// 
/// Warning：
/// -通过UMod实例化预制物会有以下问题（解决方案是等待 PrefabLinkAnchorV2 初始化完毕后，再次调用相同的初始代码（Awake、OnDisable、OnEnable））：
///     -PrefabLinkAnchorV2.Awake会将物体先隐藏（此时会调用本组件的Awake、OnDisable）
///     -再进行各个组件的反序列化操作（这一步会导致本组件初始化的内容都给重置掉）
///     -最后再恢复物体的显隐状态（此时会调用本组件的 OnEnable）。
///     
/// 常用字段：
/// -UseDynamicAttach：抓取时动态创建一个空物体，可以避免生动的抓取动作
/// </summary>
public class AD_XRGrabInteractable : XRGrabInteractable
{
    #region Fix uMod Deserialize Problem
    ///——ToUpdate：为了避免初始化时间不一致导致出错，需要统一改为由Item调用ManualInit。或者改为Awake时检测是否为Mod物体，如果是就延迟初始化——

    [ContextMenu("Awake")]
    protected override void Awake()
    {
        base.Awake();
        if (UModTool.IsUModGameObject(this))//仅针对UMod打包物体才需要重新初始化
            CoroutineManager.StartCoroutineEx(IEReInit());
    }
    IEnumerator IEReInit()
    {
        //PS:因为Awake初始化的字段都给uMod反序列化字段替换，所以要延后等待Mod完成后再次初始化。后续的其他XR组件也是相同思路
        yield return null;//等待UMod初始化完成
        yield return null;//等待UMod初始化完成
        Init();
    }
    /// <summary>
    /// Todo:
    /// -在Mod时才主动调用，以便初始化（可以通过检测物体有无带有Mod相关组件来判断是否处在Mod下）
    /// 
    /// PS：
    /// -主要是修复延迟初始化导致无法找到对应组件的问题
    /// </summary>
    [ContextMenu("Init")]
    void Init()
    {
        if (!this)//空的可能原因：创建后被立即销毁
        {
            Debug.LogError($"({this.GetType()}) is null!");
            return;
        }

        //////——通过反射的方式，重新掉哟个Awake的关键初始化方法(ToDelete:直接调用base.Awake也行)——
        ////——Ref：XRBaseInteractable.Awake——
        //if (colliders.Count == 0)//检查是否已经正常初始化，避免多次调用
        //{
        //    GetComponentsInChildren(colliders);
        //    // Skip any that are trigger colliders since these are usually associated with snap volumes.
        //    // If a user wants to use a trigger collider, they must serialize the reference manually.
        //    colliders.RemoveAll(col => col.isTrigger);
        //}

        ////查找Rigidbody组件并赋值给m_Rigidbody
        //FieldInfo fieldInfo_m_Rigidbody = ReflectionTool.GetField(typeof(XRGrabInteractable), "m_Rigidbody");
        //Rigidbody m_Rigidbody = fieldInfo_m_Rigidbody.GetValue(this) as Rigidbody;
        //if (m_Rigidbody == null)
        //{
        //    if (!TryGetComponent(out m_Rigidbody))
        //        Debug.LogError("XR Grab Interactable does not have a required Rigidbody.", this);

        //    fieldInfo_m_Rigidbody.SetValue(this, m_Rigidbody);
        //}

        ////——Refer：XRGrabInteractable.Awake——
        //FieldInfo fieldInfo_m_RigidbodyColliders = ReflectionTool.GetField(typeof(XRGrabInteractable), "m_RigidbodyColliders");
        //List<Collider> m_RigidbodyColliders_Local = fieldInfo_m_RigidbodyColliders.GetValue(this) as List<Collider>;//因为该字段为私有字段，所以用反射设置该字段。因为是引用类型，所以不需要重新赋值
        //if (m_RigidbodyColliders_Local.Count == 0)//重新查找碰撞体。
        //{
        //    m_Rigidbody.GetComponentsInChildren(true, m_RigidbodyColliders_Local);
        //    for (var i = m_RigidbodyColliders_Local.Count - 1; i >= 0; i--)
        //    {
        //        if (m_RigidbodyColliders_Local[i].attachedRigidbody != m_Rigidbody)
        //            m_RigidbodyColliders_Local.RemoveAt(i);
        //    }
        //}

        base.Awake();//重新初始化碰撞体列表等字段。m_TeleportationMonitor会因为新建而替换旧的实例，所以不会导致bug
        OnDisable();
        OnEnable();//重新注册事件
        interactionLayers = UModTool.FixSerializationCallbackReceiverData(interactionLayers);//修复Lyaers反序列化出错导致无法交互的Bug
    }
    protected override void OnDestroy()
    {
        /////PS:以下方法可以避免物体被销毁时，XRInteractionManager 报错（https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples/issues/26）
        base.OnDestroy();
        OnDisable();//手动调用 UnregisterWithInteractionManager，避免InteractionManager检测不到该物体而报错
    }
    #endregion


    void OnValidate()
    {
        /// PS：
        /// 取消勾选m_RetainTransformParent，否则从Socket取出时该物体会意外作为Socket的子物体，导致FileSystem等物体无法正常序列化/反序列化
        if (retainTransformParent)
        {
            retainTransformParent = false;
        }
    }
    protected override void Grab()
    {
        //#1 缓存层级信息
        Transform cacheSceneParent = transform.parent;
        int silbingIndex = transform.GetSiblingIndex();

        //#2 抓取（会导致parent为null）
        base.Grab();//因为父方法有很多无法调用的方法，因此只能先调用

        //#3 还原层级信息
        if (cacheSceneParent)
        {
            transform.SetParent(cacheSceneParent, true);//【修改】恢复原父子层级，避免Item层级改变导致ShellController等序列化失败
            transform.SetSiblingIndex(silbingIndex);
        }
    }
}