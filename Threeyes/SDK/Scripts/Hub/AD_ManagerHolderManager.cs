using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class AD_ManagerHolderManager : ManagerHolderManager
{
    #region Static
    /// <summary>
    /// 根据命令行参数或默认设置初始化平台模式
    /// 
    /// PS：为了避免其他Manager在本Manager尚未初始化时需要检查模式，所以直接提供静态字段
    /// </summary>
    public static AD_PlatformMode ActivePlatformMode
    {
        get
        {
            if (activePlatformMode == null)
                activePlatformMode = AD_CommandLineManager.IsVRMode ? AD_PlatformMode.PCVR : AD_PlatformMode.PC;
            return activePlatformMode.Value;
        }
    }
    static AD_PlatformMode? activePlatformMode = null;
    public static AD_WindowMode ActiveWindowMode
    {
        get
        {
            if (activeWindowMode == null)
                activeWindowMode = AD_CommandLineManager.IsWindowMode ? AD_WindowMode.Window :
(AD_CommandLineManager.IsFullScreenMode ? AD_WindowMode.FullScreen : AD_WindowMode.Default);
            return activeWindowMode.Value;
        }
    }
    static AD_WindowMode? activeWindowMode = null;



    /// <summary>
    /// 输入设备发生变化
    /// </summary>
    public static event UnityAction<PlayerInput, AD_InputDeviceType> PlayerInputControlChanged;

    public static void FirePlayerInputControlChangedEvent(PlayerInput playerInput, AD_InputDeviceType inputDeviceType)
    {
        PlayerInputControlChanged.Execute(playerInput, inputDeviceType);
    }

    #endregion

    protected override void InitWorkshopItemInfoFactory()
    {
        SteamworksTool.RegisterManagerHolder(AD_WorkshopItemInfoFactory.Instance);
    }

    protected override List<IHubManagerModPreInitHandler> GetListManagerModPreInitOrder()
    {
        ///PS:以下的Manager：
        ///-ActiveController需要提前初始化，避免后续Shell/Decoration还原的物体访问到
        return new List<IHubManagerModPreInitHandler>()
        {
            AD_ManagerHolder.ShellManager,
            AD_ManagerHolder.DecorationManager,
            AD_ManagerHolder.EnvironmentManager,
            AD_ManagerHolder.PostProcessingManager
  };
    }

    protected override List<IHubManagerModInitHandler> GetListManagerModInitOrder()
    {
        if (SteamworksTool.IsSimulator)
        {
            return new List<IHubManagerModInitHandler>()
            {
            AD_ManagerHolder.CommonSettingManager,

            //AD_ManagerHolder.ModelManager,//Deinit：优先扫描并保存所有已使用的模型Mod信息
            //AD_ManagerHolder.RuntimeEditorManager,//需要优先初始化SOAssetPack等数据，然后 Shell/DecorationManager才能正常初始化
            AD_ManagerHolder.ShellManager,
            AD_ManagerHolder.DecorationManager,
            AD_ManagerHolder.XRManager,//执行传送等行为
            AD_ManagerHolder.EnvironmentManager,//Sun等需要基于VR的位置进行计算，所以要延后初始化
            AD_ManagerHolder.PostProcessingManager
         };
        }
        else
        {
            return new List<IHubManagerModInitHandler>()
            {
            AD_ManagerHolder.CommonSettingManager,

            AD_ManagerHolder.ModelManager,//Deinit：优先扫描并保存所有已使用的模型Mod信息
            AD_ManagerHolder.RuntimeEditorManager,//需要优先初始化SOAssetPack等数据，然后 Shell/DecorationManager才能正常初始化
            AD_ManagerHolder.ShellManager,
            AD_ManagerHolder.DecorationManager,
            AD_ManagerHolder.XRManager,//执行传送等行为
            AD_ManagerHolder.EnvironmentManager,//Sun等需要基于VR的位置进行计算
            AD_ManagerHolder.PostProcessingManager
         };
        }
    }
}