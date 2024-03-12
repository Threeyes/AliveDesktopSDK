using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using Threeyes.Steamworks;
using Threeyes.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// 用户控制汽车输入
/// 
/// Todo:
/// -提炼出通用的基类，
/// -监听XRInteractable的Use，基于选中该物体的，并且切换Active（或者参考AD_RigAttachable，提供专门的右键菜单）
/// 
/// Warning：
/// -【PC模式】下，LeftControllerMove传入的值为1、0、0.75XXX（如按下前右），因为其模拟的是推杆的斜向值
/// 
/// ToUpdate：
/// -应该只是禁用对应Controller的输入，而不是禁用所有，方便操控多个物体
/// -如果对另一辆车调用驾驶，则本车应该停用（可以使用一个静态字段或回调实现，类似DP）
/// </summary>
public class AD_XRCarUserInput : MonoBehaviour
     , IContextMenuProvider//由XRManager处理右键菜单信息，因为涉及多语言
{
    public CarController carController;
    public UnityEvent onActive;
    public UnityEvent onDeactive;
    public BoolEvent onActiveDeactive;

    //#Runtime
    bool isActive;

    private void OnDestroy()
    {
        //在销毁时，如果正在激活，则需要重置，否则Rig可能无法正常移动
        if (isActive)
            ActiveFunc(false);
    }

    void ActiveFunc(bool isActive)
    {
        if (isActive)
        {
            ///Todo：
            ///-在Acitve时进行事件回调，方便通过材质发亮等让用户知道该车辆正在使用中。
            ///-临时禁止用户移动，并且监听用户的输入来控制车辆行走（实现：参考 ActionBasedControllerManager.OnRaySelectEntered中，通过调用 DisableLocomotionActions 可实现停止用户移动。可以新增ADActionBasedControllerManager子类，并通过XRManager.EnableLocomotion进行调用）（或者是修改AD_DynamicMoveProvider的相关属性）（注意：在加载Scene后需要重置，避免XR因此而无法移动！）
            ///-
            onActive.Invoke();
            onActiveDeactive.Invoke(true);
            AD_ManagerHolder.XRManager.SetLocomotion(false);//临时禁止移动（ToUpdate：如果锁定，就在状态栏上显示，图标为（两脚加斜线））
        }
        else
        {
            onDeactive.Invoke();
            onActiveDeactive.Invoke(false);

            AD_ManagerHolder.XRManager.SetLocomotion(true);//恢复移动
        }
        this.isActive = isActive;
    }

    private void Update()
    {
        if (!isActive)
            return;

        Vector2 leftHandMoveInput = AD_ManagerHolder.InputManager.LeftController2DAxis;
        carController.SetSteering(leftHandMoveInput.x);
        carController.SetAccelerate(leftHandMoveInput.y);
        carController.SetBoost(AD_ManagerHolder.InputManager.SpeedUpButtonPressed);
        carController.SetBrake(AD_ManagerHolder.InputManager.JumpButtonPressed);
    }

    #region IContextMenuProvider
    public List<ToolStripItemInfo> GetContextMenuInfo()
    {
        List<ToolStripItemInfo> listInfo = new List<ToolStripItemInfo>();
        listInfo.Add(new ToolStripItemInfo(isActive ? "Cancel Dirve" : "Drive", (o, arg) => ActiveFunc(!isActive)));
        return listInfo;
    }
    #endregion
}