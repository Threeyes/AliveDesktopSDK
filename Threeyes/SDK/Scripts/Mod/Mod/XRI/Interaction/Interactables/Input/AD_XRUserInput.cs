using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using UnityEngine;
using UnityEngine.Events;
/// <summary>
/// 
/// PS:
/// -通过回调进行启用/禁用，方便由Manager进行统一管理
/// </summary>
public interface IAD_XRUserInput
{
    void OnRegistered(EventArgs args);
    void OnUnregistered(EventArgs args);
}
/// <summary>
/// 子类需要捕捉用户的输入
/// 
/// Todo：
/// -通过EventArgs传递用户输入信息
/// -自定义需要捕捉的按键（比如只捕捉移动、只捕捉按键。不捕捉所有按键，方便用户同时进行正常移动等其他操作）
/// </summary>
public abstract class AD_XRUserInput : MonoBehaviour, IAD_XRUserInput
{
    public UnityEvent onActive;
    public UnityEvent onDeactive;
    public BoolEvent onActiveDeactive;

    //#Runtime
    protected bool isActive;

    public virtual void OnRegistered(EventArgs args)
    {
        onActive.Invoke();
        onActiveDeactive.Invoke(true);
        isActive = true;
    }

    public virtual void OnUnregistered(EventArgs args)
    {
        onDeactive.Invoke();
        onActiveDeactive.Invoke(false);
        isActive = false;
    }

    protected virtual void Active(bool isActive)
    {
        if (isActive)
        {
            AD_ManagerHolder.XRManager.RegisterUserInput(this);//标记为需要捕获用户输入，成功后会临时禁止移动（PS：如果锁定状态，就会在状态栏上显示，图标为（两脚加斜线））
        }
        else
        {
            AD_ManagerHolder.XRManager.UnRegisterUserInput(this);//恢复移动
        }
    }

    protected virtual void OnDestroy()
    {
        //在销毁时，如果正在激活，则需要重置，否则Rig可能无法正常移动
        if (isActive)
            Active(false);
    }
}
