#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class AD_AssistantManagerSimulator : AssistantManagerSimulator
{
    public Transform tfInfoGroup;//显示Mod信息

    public Toggle toggleEnableFly;
    public Toggle togglePenetrateOnFly;
    public Toggle toggleUseGravity;
    public Button buttonResetPose;

    AD_SOEditorSettingManager SOEditorSettingManagerInst { get { return AD_SOEditorSettingManager.Instance; } }
    private void OnEnable()
    {
        ShowGameobjectWithoutSaving(tfInfoGroup.gameObject, SOEditorSettingManagerInst.HubSimulator_ShowAssistantInfo);

        toggleEnableFly.SetIsOnWithoutNotify(SOEditorSettingManagerInst.HubSimulator_EnableFly);
        togglePenetrateOnFly.SetIsOnWithoutNotify(SOEditorSettingManagerInst.HubSimulator_IsPenetrateOnFly);
        toggleUseGravity.SetIsOnWithoutNotify(SOEditorSettingManagerInst.HubSimulator_UseGravity);
    }

    #region UI Callback
    public void OnEnableFlyToggleChanged(bool isOn)
    {
        AD_ManagerHolder.XRManager.SetMovementType(toggleEnableFly.isOn, togglePenetrateOnFly.isOn, toggleUseGravity.isOn);
    }
    public void OnPenetrateOnFlyToggleChanged(bool isOn)
    {
        AD_ManagerHolder.XRManager.SetMovementType(toggleEnableFly.isOn, togglePenetrateOnFly.isOn, toggleUseGravity.isOn);
    }
    public void OnUseGravityToggleChanged(bool isOn)
    {
        AD_ManagerHolder.XRManager.SetMovementType(toggleEnableFly.isOn, togglePenetrateOnFly.isOn, toggleUseGravity.isOn);
    }
    public void OnResetPoseButtonClick()
    {
        AD_ManagerHolder.XRManager.ResetRigPose();
    }
    #endregion

    #region Override
    //因为某些原因需要临时隐藏UI（如截图）
    public override void TempShowInfoGroup(bool isShow)
    {
        if (!isShow)
        {
            ShowGameobjectWithoutSaving(tfInfoGroup.gameObject, false);
        }
        else
        {
            ShowGameobjectWithoutSaving(tfInfoGroup.gameObject, AD_SOEditorSettingManager.Instance.HubSimulator_ShowAssistantInfo);//根据设置决定是否临时显示
        }
    }
    #endregion
}
#endif