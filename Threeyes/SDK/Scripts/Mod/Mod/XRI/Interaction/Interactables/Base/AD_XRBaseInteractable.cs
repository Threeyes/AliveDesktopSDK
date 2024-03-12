using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Threeyes.Config;
using Threeyes.Core;
using Threeyes.Coroutine;
using Threeyes.Data;
using Threeyes.Persistent;
using Threeyes.RuntimeSerialization;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// 该组件主要是修复UMod的bug，AD_XRSlider等需要继承XRBaseInteractable的自定义组件可以改为继承该组件
/// </summary>
public class AD_XRBaseInteractable : XRBaseInteractable
{
    #region Fix uMod Deserialize Problem
    protected override void Awake()
    {
        base.Awake();

        if (UModTool.IsUModGameObject(this))
            CoroutineManager.StartCoroutineEx(IEReInit());
        else
            InitFunc();
    }
    IEnumerator IEReInit()
    {
        yield return null;//等待UMod初始化完成
        yield return null;//等待UMod初始化完成

        //ReInit
        base.Awake();
        OnDisable();
        OnEnable();
        interactionLayers = UModTool.FixSerializationCallbackReceiverData(interactionLayers);//修复Lyaers反序列化出错导致无法交互的Bug

        InitFunc();
    }
    protected virtual void InitFunc()
    {
        //方便子类添加一些需要在Awake执行的方法
    }
    #endregion

    protected virtual void OnValidate()
    {

    }
}

/// <summary>
/// 可缓存数据的XR组件
/// 
/// PS:
/// -因为AC不使用该组件，且不需要通过PD暴露字段，因此暂不提供TSOConfig等会增加复杂性的字段
/// </summary>
[RequireComponent(typeof(RuntimeSerializable_GameObject))]
public abstract class AD_XRBaseInteractable<TContainer, TConfig, TPropertyBag> : AD_XRBaseInteractable
    , IConfigurableComponent<TConfig>
    , IRuntimeSerializableComponent
    where TContainer : Component, IConfigurableComponent<TConfig>
    where TConfig : SerializableComponentConfigInfoBase, new()
    where TPropertyBag : ComponentPropertyBag<TContainer>, new()
{
    #region Unity Method
    protected override void Awake()
    {
        base.Awake();
        Config.actionPersistentChanged += OnPersistentChanged;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Config.actionPersistentChanged -= OnPersistentChanged;
    }
    #endregion

    #region IModHandler
    public void OnModInit()
    {
        UpdateSetting();
    }
    public void OnModDeinit() { }
    void OnPersistentChanged(PersistentChangeState persistentChangeState)
    {
        UpdateSetting();
    }
    public abstract void UpdateSetting();
    #endregion

    #region IConfigurableComponent
    public virtual TConfig Config { get { return defaultConfig; } }

    public TConfig DefaultConfig { get { return defaultConfig; } set { defaultConfig = value; } }
    [Header("Config")]
    [SerializeField] protected TConfig defaultConfig = new TConfig();//Default config
    #endregion

    #region IRuntimeSerializableComponent
    public Identity ID { get { return id; } set { } }
    [SerializeField] protected Identity id = new Identity();
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        RuntimeSerializationTool.EditorUpdateComponetID(this, ref id);
    }
#endif

    public Type ContainerType { get { return GetType(); } }
    public IComponentPropertyBag ComponentPropertyBag { get { return GetSerializePropertyBag(); } }
    public virtual string Serialize()
    {
        TPropertyBag propertyBag = GetSerializePropertyBag();
        return RuntimeSerializationTool.SerializeObject(propertyBag);
    }
    public virtual TPropertyBag GetSerializePropertyBag()
    {
        TPropertyBag propertyBag = new TPropertyBag();
        propertyBag.Init(this as TContainer);//Warning：需要使用as转为真实类型，确保containerTypeName会被初始化。具体逻辑在Init中实现
        return propertyBag;
    }
    public virtual void Deserialize(string content, IDeserializationOption baseOption = null)
    {
        TPropertyBag propertyBag = default(TPropertyBag);
        if (content.NotNullOrEmpty())
        {
            propertyBag = JsonConvert.DeserializeObject<TPropertyBag>(content);
        }
        DeserializeFunc(propertyBag);
    }
    public virtual void DeserializeBase(IComponentPropertyBag basePropertyBag, IDeserializationOption baseOption = null)
    {
        if (basePropertyBag is TPropertyBag realPropertyBag)
        {
            DeserializeFunc(realPropertyBag);
        }
    }
    public virtual void DeserializeFunc(TPropertyBag propertyBag)
    {
        TContainer inst = this as TContainer;
        propertyBag?.Accept(ref inst);
    }
    #endregion
}
