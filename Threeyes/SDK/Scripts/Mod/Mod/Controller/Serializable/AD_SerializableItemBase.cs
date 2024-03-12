using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threeyes.Core;
using Threeyes.Data;
using Threeyes.Localization;
using Threeyes.Persistent;
using Threeyes.RuntimeEditor;
using Threeyes.RuntimeSerialization;
using Threeyes.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Base class for all Serializable item
/// 
/// PS：针对需要持久化属性的元件
/// </summary>
public abstract class AD_SerializableItemBase<TElement, TEleData, TPropertyBag> : ElementBase<TEleData>
        , IAD_SerializableItem
    where TElement : AD_SerializableItemBase<TElement, TEleData, TPropertyBag>
    where TEleData : AD_SerializableItemInfo
    where TPropertyBag : ComponentPropertyBag<TElement>, new()
{
    #region Property & Field
    protected static bool IsRuntimeEditorMode { get { return RuntimeEditorManagerHolder.RuntimeEditorManager.IsActive; } }
    public IAD_SerializableItemInfo BaseData { get { return data; } }
    public RuntimeSerializable_GameObject RuntimeSerialization_GameObject
    {
        get
        {
            if (!runtimeSerialization_GameObject)
                runtimeSerialization_GameObject = GetComponent<RuntimeSerializable_GameObject>();
            return runtimeSerialization_GameObject;
        }
    }
    public RuntimeSerializable_GameObject runtimeSerialization_GameObject;//管理该物体的序列化信息，以及标记Prefab
    #endregion

    #region Unity Method
    //protected virtual void Awake()
    //{
    //    //TrySaveDefaultData();//注释原因：因为data的数据只有在调用Init时才有效，所以与ConfigurableComponentBaseEx不同，不需要在Awake保存
    //}
    #endregion

    #region IAD_SerializableItem
    public void DelayActiveFeature(float delayTime)
    {
        StartCoroutine(IEDelayActiveFeature(delayTime));
    }
    IEnumerator IEDelayActiveFeature(float delayTime)
    {
        //#1 缓存初始状态并禁用Interactor，避免在创建时Socket意外吸取其他物体
        List<XRBaseInteractor> listInteractor = transform.FindComponentsInChild<XRBaseInteractor>(true, true);
        Dictionary<XRBaseInteractor, bool> pairsInteractorActive = listInteractor.ToDictionary(keySelector: i => i, elementSelector: i => i.enabled);
        listInteractor.ForEach(i => i.enabled = false);

        yield return new WaitForSecondsRealtime(delayTime);

        //#2 还原Interactor的初始状态
        foreach (var kv in pairsInteractorActive)
        {
            if (kv.Key)
                kv.Key.enabled = kv.Value;
        }
    }
    #endregion

    #region Init
    public override void InitFunc(TEleData incomeData)
    {
        //ToUpdate:在TEleData中，增加不序列化的当前操作枚举：Init/Restore/Refresh/ChangeStyle。删掉IsBaseType

        //拷贝必要成员数据
        if (data != null)//data不为空代表已经初始化
        {
            ///PS:
            ///-如果输入为基类AD_FileSystemItemInfo，则仅拷贝基类的字段；否则拷贝所有字段（也就是只拷贝有效字段，避免自定义字段被覆盖）
            if (incomeData.IsBaseType)//Refresh：传入的是基类，这时不要覆盖已有的数据类，而是仅复制父类公有的部分，子类中保留用户自定义字段；
            {
                data.CopyBaseMembersFrom(incomeData);
            }
            else//ChangeStyle：相同的子类类型，可以直接复制其所有字段
            {
                //【ToUpdate】：后续可以参考Unity复制Event的实现，将组件利用序列化/反序列化来初始化，即OnDeserializeRaw(otherInst.GetSerializePropertyBag())
                data.CopyAllMembersFrom(incomeData);
            }
        }
        else//如果某个实例为空，则直接使用输入的数据
        {
            base.InitFunc(incomeData);
        }

        TrySaveDefaultData();//保存此时传入的数据
    }

    void OnPersistentChanged(PersistentChangeState persistentChangeState)
    {
        UpdateSetting();
    }
    public abstract void UpdateSetting();
    #endregion

    #region IRuntimeEditable
    public FilePathModifier FilePathModifier { get; set; }
    public bool IsRuntimeEditable { get { return true; } }
    public virtual string RuntimeEditableDisplayName { get { return null; } }

    public virtual List<RuntimeEditableMemberInfo> GetRuntimeEditableMemberInfos()
    {
        List<ToolStripItemInfo> listInfo = new List<ToolStripItemInfo>()
            {
            new ToolStripItemInfo(LocalizationManagerHolder.LocalizationManager.GetTranslationText("Browser/ItemInfo/Common/Reset"), (obj, arg) => ResetDefaultConfig())
            };

        return new List<RuntimeEditableMemberInfo>()
            {
                new RuntimeEditableMemberInfo(this, this.GetType(), nameof(data), listContextItemInfo:listInfo)
            };
    }

    public virtual void InitRuntimeEditable(FilePathModifier filePathModifier)
    {
        //#1 初始化参数
        FilePathModifier = filePathModifier;

        //#2 重新监听的PD事件
        ReRegisterDataEvent();

        //#3 模拟PD的初始化流程，通知data更新相关字段，以及读取外部资源，通过回调OnPersistentChanged调用UpdateSetting方法
        PersistentObjectTool.ForceInit(data, FilePathModifier.ParentDir);
    }

    /// <summary>
    /// 重新注册PD的回调
    /// </summary>
    protected virtual void ReRegisterDataEvent()
    {
        //确保回调不会被多次监听
        data.actionPersistentChanged -= OnPersistentChanged;
        data.actionPersistentChanged += OnPersistentChanged;
    }

    [SerializeField] protected TEleData cacheDefaultData = null;//缓存组件序列化之前的Default数据，方便还原
    bool hasSaveDefaultData = false;
    /// <summary>
    /// 尝试保存DefaultConfig，便于UIRuntimeEdit重置
    /// </summary>
    void TrySaveDefaultData()
    {
        if (hasSaveDefaultData)
            return;
        ///Warning：
        ///-之所以不使用PropertyBag来保存cacheDefaultData，是因为数据可能会发生改变，而且如果上次保存时没有实现该功能，则数据为空。通过在Init时调用该方法能确保cacheDefaultData被初始化
        cacheDefaultData = UnityObjectTool.DeepCopy(data);
        cacheDefaultData.ResetUserCustomData();//Warning：因为data是运行时生成而不是场景中预设的数据，在Deserialize后通过InitFunc传入的data为上次修改的持久化数据而不是首次初始化的数据，因此需要清空cacheDefaultData中用户自定义部分
        hasSaveDefaultData = true;
    }
    void ResetDefaultConfig()
    {
        if (cacheDefaultData == null)
        {
            Debug.LogError($"{nameof(cacheDefaultData)} not init!");
            return;
        }
        PersistentObjectTool.ForceInit(data, FilePathModifier.ParentDir, cacheDefaultData);
    }
    #endregion

    #region RuntimeSerializableComponent

    #region ID  标记该组件的唯一ID，便于绑定。
    public Identity ID { get { return id; } set { } }
    [SerializeField] protected Identity id = new Identity();
#if UNITY_EDITOR
    void OnValidate() { RuntimeSerializationTool.EditorUpdateComponetID(this, ref id); }
#endif
    #endregion
    public System.Type ContainerType { get { return GetType(); } }
    public IComponentPropertyBag ComponentPropertyBag { get { return GetPropertyBag(); } }
    protected virtual Formatting Formatting { get { return Formatting.None; } }
    public virtual string Serialize()
    {
        TPropertyBag propertyBag = GetPropertyBag();
        return RuntimeSerializationTool.SerializeObject(propertyBag, Formatting);
    }
    public virtual TPropertyBag GetPropertyBag()
    {
        //PS:泛型构造函数只能调用无参，只能通过以下方式初始化
        TPropertyBag propertyBag = new TPropertyBag();
        propertyBag.Init(this as TElement);//Warning：需要使用as转为真实类型，确保containerTypeName会被初始化。具体逻辑在Init中实现
        return propertyBag;
    }
    public virtual void Deserialize(string content, IDeserializationOption baseOption = null)
    {
        TPropertyBag propertyBag = default(TPropertyBag);
        if (content.NotNullOrEmpty())
        {
            propertyBag = JsonConvert.DeserializeObject<TPropertyBag>(content);
            DeserializeFunc(propertyBag, baseOption);
        }
        else
        {
            Debug.LogError($"Deserialization Content for {ContainerType} is null!");
        }
    }
    public virtual void DeserializeBase(IComponentPropertyBag basePropertyBag, IDeserializationOption baseOption = null)
    {
        if (basePropertyBag is TPropertyBag realPropertyBag)
            DeserializeFunc(realPropertyBag, baseOption);
    }
    protected virtual void DeserializeFunc(TPropertyBag propertyBag, IDeserializationOption baseOption = null)
    {
        //TrySaveDefaultData();//注释原因：Controller后期会调用InitFunc，从而调用此方法

        TElement inst = this as TElement;
        propertyBag?.Accept(ref inst);
    }
    #endregion

    #region Dispose
    protected override void OnDespawnFunc()
    {
        base.OnDespawnFunc();

        //在游戏对象被销毁时，销毁data的数据
        //PS：即使是ChangeStyle，新的Item也会进行重新加载（因为PD会调用Load，且暂时不判断资源是否一致）
        data?.Dispose();
    }
    #endregion
}

/// <summary>
/// 针对FileSystem/Decoration等需要实现右键的元件
/// </summary>
/// <typeparam name="TManager"></typeparam>
/// <typeparam name="TSOPrefabInfo"></typeparam>
/// <typeparam name="TElement"></typeparam>
/// <typeparam name="TEleData"></typeparam>
/// <typeparam name="TSOConfig"></typeparam>
/// <typeparam name="TConfig"></typeparam>
public abstract class AD_SerializableItemWithContextMenuBase<TElement, TEleData, TPropertyBag> : AD_SerializableItemBase<TElement, TEleData, TPropertyBag>
, IAD_SerializableItemWithContextMenu
where TElement : AD_SerializableItemWithContextMenuBase<TElement, TEleData, TPropertyBag>
    where TEleData : AD_SerializableItemInfo, new()
    where TPropertyBag : AD_SerializableItemPropertyBagBase<TElement, TEleData>, new()
{
    #region IContextMenuHolder
    /// <summary>
    /// PS：方便Modder自行实现有趣的功能
    /// </summary>
    /// <returns></returns>
    public virtual List<ToolStripItemInfo> GetContextMenuInfo() { return new List<ToolStripItemInfo>(); }
    #endregion
}

[System.Serializable]
[JsonObject(MemberSerialization.Fields)]
public abstract class AD_SerializableItemPropertyBagBase<TContainer, TEleData> : ComponentPropertyBag<TContainer>
    where TContainer : ElementBase<TEleData>//确保能读写其data字段
    where TEleData : class, new()
{
    public TEleData data = new TEleData();//避免报null错

    public override void Init(TContainer container)
    {
        base.Init(container);
        data = container.data;
    }
    public override void Accept(ref TContainer container)
    {
        base.Accept(ref container);

        if (data != null)
            //container.data = data;
            UnityObjectTool.CopyFields(data, container.data);//复制全部字段，不筛选（包括Asset引用）
    }
}