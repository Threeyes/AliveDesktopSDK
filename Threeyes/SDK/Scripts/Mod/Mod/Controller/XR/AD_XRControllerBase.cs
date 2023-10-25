using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Threeyes.Config;
using System;
using Threeyes.Persistent;
using Newtonsoft.Json;
using UnityEngine.Events;
using Threeyes.RuntimeEditor;

public abstract class AD_XRControllerBase<TSOConfig, TConfig> : ConfigurableComponentBase<TSOConfig, TConfig>, IAD_XRController
    where TSOConfig : SOConfigBase<TConfig>
    where TConfig : AD_XRControllerConfigInfoBase
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
    public abstract void ResetPose();
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

    /// <summary>
    /// 在RuntimeEditor中增加按钮，用于在意外跳出场景时重置XRRig的Pose
    /// 
    /// PS：因为Config在编辑时已经被克隆，导致直接监听监听无效。所以可以直接在方法内调用XRManager的ResetPose，其会调用Controller的对应方法
    /// </summary>
    [RuntimeEditorButton]
    void ResetPose()
    {
        AD_ManagerHolder.XRManager.ResetPose();
    }
}
