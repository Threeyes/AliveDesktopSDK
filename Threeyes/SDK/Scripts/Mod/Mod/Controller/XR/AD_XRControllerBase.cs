using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Threeyes.Config;
using System;
using Threeyes.Persistent;
using Newtonsoft.Json;
using UnityEngine.Events;
using Threeyes.RuntimeEditor;
using Threeyes.Core;

public abstract class AD_XRControllerBase<TSOConfig, TConfig> : ConfigurableComponentBase<TSOConfig, TConfig>, IAD_XRController
    where TSOConfig : SOConfigBase<TConfig>
    where TConfig : AD_XRControllerConfigInfoBase, new()
{
    event UnityAction<PersistentChangeState> PersistentChanged;

    protected virtual void Awake()
    {
        Config.actionPersistentChanged += OnPersistentChanged;
    }
    protected virtual void OnDestroy()
    {
        Config.actionPersistentChanged -= OnPersistentChanged;
    }

    protected virtual void OnPersistentChanged(PersistentChangeState persistentChangeState)
    {
        PersistentChanged.Execute(persistentChangeState);
        UpdateSetting();
    }
    protected abstract void UpdateSetting();
    /// <summary>
    /// 重置XRRig到默认位置
    /// </summary>
    public abstract void ResetRigPose();
    public abstract void UpdateLocomotionSetting();
    public virtual void OnModControllerInit() { }
    public virtual void OnModControllerDeinit() { }
}

[Serializable]
[PersistentChanged(nameof(AD_XRControllerConfigInfoBase.OnPersistentChanged))]
public class AD_XRControllerConfigInfoBase : SerializableDataBase
{
    [JsonIgnore] public UnityAction<PersistentChangeState> actionPersistentChanged;
    [JsonIgnore] public UnityAction actionResetPos;

    #region Callback
    void OnPersistentChanged(PersistentChangeState persistentChangeState)
    {
        actionPersistentChanged.Execute(persistentChangeState);
    }
    #endregion
}