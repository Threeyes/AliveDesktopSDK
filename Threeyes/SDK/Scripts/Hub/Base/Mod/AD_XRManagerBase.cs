using System;
using System.Collections;
using System.Linq;
using Threeyes.Persistent;
using Threeyes.Steamworks;
using Threeyes.XRI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Management;
/// <summary>
/// 管理Rig及Camera的行为
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TDefaultController"></typeparam>
public abstract class AD_XRManagerBase<T> : HubManagerWithControllerBase<T, IAD_XRController, AD_DefaultXRController>, IAD_XRManager
        where T : AD_XRManagerBase<T>
{

    /// <summary>
    /// PS:为了避免其他Manager在ActivePlatformMode尚未初始化时需要检查模式，所以直接提供该方法
    /// </summary>
    public static bool IsCommandVRMode
    {
        get
        {
#if UNITY_EDITOR
            if (Application.isEditor)//【编辑器模式】读取Resource的配置并初始化
            {
                return AD_SOEditorSettingManager.Instance.PlatformMode == AD_PlatformMode.PCVR;
            }
            else
#endif
            {
                string[] args = Environment.GetCommandLineArgs();
                return args.ToList().Contains(commandArg_VRMode);
            }
        }
    }
    const string commandArg_VRMode = "-vrMode";

    #region Interface
    public Transform TfCameraRigParent { get { return tfCameraRigParent; } }
    public Transform TfCameraRig { get { return tfCameraRig; } }
    public Transform TfCameraEye { get { return tfCameraEye; } }
    public Camera VrCamera { get { return vrCamera; } }
    public Transform TfLeftController { get { return tfLeftController; } }
    public Transform TfRightController { get { return tfRightController; } }
    public ActionBasedController LeftController { get { return leftController; } }
    public ActionBasedController RightController { get { return rightController; } }
    public AD_DynamicMoveProvider DynamicMoveProvider { get { return dynamicMoveProvider; } }
    public XRDeviceSimulator xRDeviceSimulator;
    public AD_PlatformMode ActivePlatformMode = AD_PlatformMode.PC;//Todo：通过AD_Entry读取配置并设置该字段

    //#Runtime
    [SerializeField] Transform tfCameraRigParent;//[XR Interaction Setup]
    [SerializeField] Transform tfCameraRig;//[XR Origin(XR Rig)]
    [SerializeField] Transform tfCameraEye;
    [SerializeField] Camera vrCamera;
    [SerializeField] Transform tfLeftController;
    [SerializeField] Transform tfRightController;
    [SerializeField] ActionBasedController leftController;
    [SerializeField] ActionBasedController rightController;

    [SerializeField] AD_DynamicMoveProvider dynamicMoveProvider;

    public void TeleportTo(Vector3 position, Quaternion rotation, MatchOrientation matchOrientation)
    {
        XRITool.TeleportTo(position, rotation, matchOrientation);
    }

    public void ResetPose()
    {
        ActiveController.ResetPose();
    }
    #endregion

    #region Init

    /// <summary>
    /// 在程序初始化时调用
    /// </summary>
    protected void Init()
    {
        ////#1 初始化所有VR相关字段（PS：后续用户如果要自定义XRRig，可使用此代码）（ToUpdate：可以改为基于XROrigin获取其他物体）
        //vrCamera = transform.FindFirstComponentInChild<Camera>(false, true, c => c.GetComponent<TrackedPoseDriver>() != null);
        //if (vrCamera)
        //{
        //    tfCameraEye = vrCamera.transform;
        //    tfCameraRigParent = tfCameraRig.parent;

        //    leftController = tfCameraRig.FindFirstComponentInChild<ActionBasedController>(false, true, (c) => c.gameObject.name.StartsWith("Left"));
        //    tfLeftController = leftController?.transform;
        //    rightController = tfCameraRig.FindFirstComponentInChild<ActionBasedController>(false, true, (c) => c.gameObject.name.StartsWith("Right"));
        //    tfRightController = rightController?.transform;
        //}

        //根据命令行参数初始化平台模式
        ActivePlatformMode = IsCommandVRMode ? AD_PlatformMode.PCVR : AD_PlatformMode.PC;
        SetupPlatformMode(ActivePlatformMode);
    }


    private void OnApplicationQuit()
    {
        //VR模式退出时要Stop，否则会卡住
        if (ActivePlatformMode == AD_PlatformMode.PCVR)
            StopXR();
    }
    /// <summary>
    /// 切换VR模式
    /// 
    /// PS:暂时只能在启动时设置
    /// Todo：
    /// 【V2】后期查找如何在运行时切换模式（如缓存InputSystem.devices中所有的XRHMD）
    /// </summary>
    /// <param name="platformMode"></param>
    public void SetupPlatformMode(AD_PlatformMode platformMode)
    {
        xRDeviceSimulator.enabled = platformMode == AD_PlatformMode.PC;//XRDeviceSimulator会在OnEnable时，自动移除其他VR设备

        //如果OpenXR没有设置为InitializeXRonStartup，则需要手动调用此方法来初始化OpenXR
        if (platformMode == AD_PlatformMode.PCVR)
        {
            StartXR();
        }
    }

    #region Utility
    bool hasManualStartXR = false;

    public void StartXR()
    {
        StartCoroutine(IEStartXR());
    }
    IEnumerator IEStartXR()
    {
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)//避免重复初始化
            yield break;

        hasManualStartXR = true;
        Debug.Log("Initializing XR...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            LogError("Log/ErrorInfo/XR/InitXRFailed", "Initializing XR Failed. Check Player log for details. Possible cause: VR device not connected.");
        }
        else
        {
            Debug.Log("Starting XR...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }

    void StopXR()
    {
        if (!hasManualStartXR)
            return;
        Debug.Log("Stopping XR...");

        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        Debug.Log("XR stopped completely.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="translationName">多语言Key</param>
    /// <param name="errorInfo">错误信息</param>
    protected virtual void LogError(string translationName, string errorInfo)
    {
        Debug.LogError(errorInfo);
    }
    #endregion
    #endregion


    #region Callback
    public void OnModInit(Scene scene, ModEntry modEntry)
    {
        modController = scene.GetComponents<IAD_XRController>().FirstOrDefault();
        defaultController.gameObject.SetActive(modController == null);

        ActiveController.OnModControllerInit();//初始化
        ManagerHolderManager.Instance.SetGlobalControllerConfigState<IAD_SOXRControllerConfig>(modController == null);//设置对应的全局Config是否可用
    }

    public void OnModDeinit(Scene scene, ModEntry modEntry)
    {
        modController?.OnModControllerDeinit();//仅DeinitMod的Controller
        modController = null;
    }
    #endregion


    #region Controller Callback
    protected virtual void OnPersistentChanged(PersistentChangeState persistentChangeState)
    {
    }
    #endregion
}