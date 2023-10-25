using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using Threeyes.Data;
using Threeyes.Persistent;
using Threeyes.RuntimeEditor;
using Threeyes.RuntimeSerialization;
using Threeyes.Steamworks;
using Threeyes.UI;

/// <summary>
/// Base class for all Serializable item
/// 
/// PS：针对需要持久化属性的元件
/// </summary>
public abstract class AD_SerializableItemBase<TElement, TEleData, TPropertyBag> : ElementBase<TEleData>
        , IAD_SerializableItem
        , IRuntimeSerializationComponent
    where TElement : AD_SerializableItemBase<TElement, TEleData, TPropertyBag>
    where TEleData : AD_SerializableItemInfo
    where TPropertyBag : PropertyBag<TElement>, new()
{
    #region Property & Field
    public IAD_SerializableItemInfo BaseData { get { return data; } }
    public RuntimeSerialization_GameObject RuntimeSerialization_GameObject
    {
        get
        {
            if (!runtimeSerialization_GameObject)
                runtimeSerialization_GameObject = GetComponent<RuntimeSerialization_GameObject>();
            return runtimeSerialization_GameObject;
        }
    }
    public RuntimeSerialization_GameObject runtimeSerialization_GameObject;//管理该物体的序列化信息，以及标记Prefab
    #endregion

    #region Init
    public override void InitFunc(TEleData incomeData)
    {
        //#1 拷贝必要成员数据
        if (data != null && incomeData != null)
        {
            ///PS:
            ///-如果输入为基类AD_FileSystemItemInfo，则仅拷贝基类的字段；否则拷贝所有字段（也就是只拷贝有效字段，避免自定义字段被覆盖）
            if (incomeData.IsBaseType)//Restore或Refresh时传入的是基类，这时不要覆盖已有的数据类，而是仅复制父类公有的部分，子类中保留用户自定义字段；
            {
                data.CopyBaseMembersFrom(incomeData);
            }
            else//ChangeStyle是相同的子类，可以直接复制其原值
            {
                data.CopyAllMembersFrom(incomeData);
            }
        }
        else//如果某个实例为空，则直接使用输入的数据（ToTest）
        {
            base.InitFunc(incomeData);
        }

        //#2 初始化PD

        //——RuntimeEdit——

        //#1监听PD更新(确保InitFunc被多次调用时，不会导致多次监听)
        data.PersistentChanged -= OnPersistentChanged;
        data.PersistentChanged += OnPersistentChanged;

        //PS:要模拟PersistentDataComplexBase，在初始化完成后调用PersistentObjectTool.CopyFiledsAndLoadAsset(PersistentChangeState.Load)

        //#2通过PD的回调来调用UpdateSetting进行更新
        if (data != null)//针对旧数据：
        {
            var originClone = UnityObjectTool.DeepCopy(data);
            PersistentObjectTool.CopyFiledsAndLoadAsset(data, originClone, PersistentChangeState.Load, FilePathModifier.ParentDir);
        }
        else//针对初次初始化
        {
            UpdateSetting();
        }
    }
    void OnPersistentChanged(PersistentChangeState persistentChangeState)
    {
        UpdateSetting();
    }
    protected abstract void UpdateSetting();

    #endregion

    #region IRuntimeSerializationComponent
    //用于序列化/反序列化
    public System.Type ContainerType { get { return GetType(); } }
    public virtual string OnSerialize()
    {
        TPropertyBag propertyBag = GetSerializePropertyBag();
        string result = JsonConvert.SerializeObject(propertyBag, RuntimeSerializationTool.DefaultComponentFormatting, RuntimeSerializationTool.DefaultJsonSerializerSettings);
        return result;
    }
    public virtual TPropertyBag GetSerializePropertyBag()
    {
        //PS:泛型构造函数只能调用无参，只能通过以下方式初始化
        TPropertyBag propertyBag = new TPropertyBag();
        propertyBag.Init(this as TElement);//传入真实类型，确保containerTypeName会被初始化。具体逻辑在Init中实现
        return propertyBag;
    }
    public virtual void OnDeserialize(string content)
    {
        TPropertyBag propertyBag = default(TPropertyBag);
        if (content.NotNullOrEmpty())
        {
            propertyBag = JsonConvert.DeserializeObject<TPropertyBag>(content);
        }
        OnDeserializeRaw(propertyBag);
    }
    public virtual void OnDeserializeRaw(TPropertyBag propertyBag)
    {
        TElement inst = this as TElement;
        propertyBag?.Accept(ref inst);
    }
    #endregion

    #region IRuntimeEditable
    public FilePathModifier FilePathModifier { get; private set; }
    public virtual void InitRuntimeEdit(FilePathModifier filePathModifier)
    {
        FilePathModifier = filePathModifier;
    }

    public virtual List<RuntimeEditableMemberInfo> GetListRuntimeEditableMemberInfo()
    {
        return new List<RuntimeEditableMemberInfo>()
        {
            new RuntimeEditableMemberInfo(this,this.GetType(),nameof(data))
        };
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
    protected static bool IsRuntimeEditorMode { get { return RuntimeEditorManagerHolder.RuntimeEditorManager.IsActive; } }

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
public abstract class AD_SerializableItemPropertyBagBase<TElement, TEleData> : PropertyBag<TElement>
    where TElement : ElementBase<TEleData>
    where TEleData : class, new()
{
    public TEleData data = new TEleData();//避免报错

    public override void Init(TElement container)
    {
        base.Init(container);
        data = container.data;
    }
    public override void Accept(ref TElement container)
    {
        base.Accept(ref container);

        if (data != null)
            container.data = data;
    }
}

