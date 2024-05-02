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

    [ContextMenu("Awake")]
    protected override void Awake()
    {
        base.Awake();

        //为了避免初始化时间不一致导致出错，针对UMod打包物体延迟初始化
        if (UModTool.IsUModGameObject(this))
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
    /// 主要是修复延迟初始化导致无法初始化对应组件（如碰撞体）的问题
    /// </summary>
    [ContextMenu("Init")]
    void Init()
    {
        if (!this)//空的可能原因：创建后被立即销毁
        {
            //Debug.LogError($"({this.GetType()}) is null! This error can be ignore!");
            return;
        }

        base.Awake();//重新初始化碰撞体列表等字段。m_TeleportationMonitor会因为新建而替换旧的实例，所以不会导致bug
        OnDisable();
        OnEnable();//重新注册事件
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