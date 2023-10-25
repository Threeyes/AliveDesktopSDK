using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Threeyes.Config;
using Threeyes.Persistent;
using UnityEngine;
using UnityEngine.Events;

[PersistentChanged(nameof(AD_SerializableItemInfo.OnPersistentChanged))]
public abstract class AD_SerializableItemInfo : SerializableDataBase, IAD_SerializableItemInfo, System.IDisposable
{
    #region IAD_SerializableItemInfo
    [DefaultValue(true)] [JsonIgnore] public bool IsDestroyRuntimeAssetsOnDispose { get; set; }//【Runtime】【Don't Copy】
    /// <summary>
    /// 标记该实例的原型是否为基类（可以确认并决定需要拷贝哪些有效成员）
    /// 
    /// 适用于：
    /// -只需要基类的成员，保留自身的自定义成员（如Restore）
    /// </summary>
    [JsonIgnore] public bool IsBaseType { get; set; }//【Runtime】【Don't Copy】

    public event UnityAction<PersistentChangeState> PersistentChanged { add { _PersistentChanged += value; } remove { _PersistentChanged -= value; } }//PS：声明为Property是为了使用接口
    [JsonIgnore] UnityAction<PersistentChangeState> _PersistentChanged;

    public abstract void CopyBaseMembersFrom(object otherInst);
    public virtual void CopyAllMembersFrom(object otherInst)
    {
        CopyBaseMembersFrom(otherInst);
        //子类继续拷贝剩余字段...
    }
    #endregion

    #region IDisposable
    /// <summary>
    /// 物体销毁被调用
    /// </summary>
    public void Dispose()
    {
        if (IsDestroyRuntimeAssetsOnDispose)
            DestroyRuntimeAssets();
    }

    /// <summary>
    /// 卸载运行时加载的资源（如图像）
    /// </summary>
    public abstract void DestroyRuntimeAssets();
    #endregion

    #region Callback
    void OnPersistentChanged(PersistentChangeState persistentChangeState)
    {
        _PersistentChanged.Execute(persistentChangeState);
    }

    #endregion
}