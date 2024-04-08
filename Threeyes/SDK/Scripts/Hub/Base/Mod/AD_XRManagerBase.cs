using System;
using System.Collections;
using System.Linq;
using Threeyes.Core;
using Threeyes.Persistent;
using Threeyes.Steamworks;
using Threeyes.XRI;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
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
    #region Interface
    public Transform TfCameraRigParent { get { return tfCameraRigParent; } }
    public Transform TfCameraRig { get { return tfCameraRig; } }
    public Transform TfCameraEye { get { return tfCameraEye; } }
    public Camera VrCamera { get { return vrCamera; } }
    public Transform TfLeftController { get { return tfLeftController; } }
    public Transform TfRightController { get { return tfRightController; } }
    public ActionBasedController LeftController { get { return leftController; } }
    public ActionBasedController RightController { get { return rightController; } }

    public bool EnableLocomotion { get => dynamicMoveProvider.enabled; }
    public bool EnableFly { get => dynamicMoveProvider.enableFly; protected set => dynamicMoveProvider.enableFly = value; }
    public bool UseGravity { get => dynamicMoveProvider.useGravity; protected set => dynamicMoveProvider.useGravity = value; }

    //public AD_DynamicMoveProvider DynamicMoveProvider { get { return dynamicMoveProvider; } }//PS：暂不提供，等后续用户有需要自定义再公开

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
    [SerializeField] CharacterController characterController;
    public virtual void SetLocomotion(bool isEnable)
    {
        //PS:不调用ActionBasedControllerManager.UpdateLocomotionActions/DisableLocomotionActions，是因为该实现不需要反射，且确保相关Action正常运行（比如用于驾驶）
        if (dynamicMoveProvider)//避免程序退出时该实例被销毁导致报错
            dynamicMoveProvider.enabled = isEnable;
    }

    public void SetMovementType(bool enableFly, bool isPenetrateOnFly, bool useGravity)
    {
        //在Rig附着时，临时禁止修改移动方式
        if (IsRigAttaching)
        {
            Debug.LogWarning("RigAttaching! Ignore SetMovementType!");
            return;
        }

        SetMovementTypeFunc(enableFly, isPenetrateOnFly, useGravity);
    }

    protected virtual void SetMovementTypeFunc(bool enableFly, bool isPenetrateOnFly, bool useGravity)
    {
        characterController.excludeLayers = enableFly && isPenetrateOnFly ? -1 : 0;//如果在飞行时需要忽略碰撞体，则将excludeLayers设置为Everything；否则设置为Nothing；
        EnableFly = enableFly;
        UseGravity = useGravity;
    }

    public void TeleportTo(Vector3 position, Quaternion rotation, MatchOrientation matchOrientation, AD_XRDestinationRigPart destinationRigPart = AD_XRDestinationRigPart.Foot)
    {
        Vector3 targetPos = position;
        if (destinationRigPart == AD_XRDestinationRigPart.Head)
        {
            //让头与目标点位于同一高度
            Vector3 eyeOffset = TfCameraRig.position - TfCameraEye.position;
            targetPos += eyeOffset;
        }

        XRITool.TeleportTo(targetPos, rotation, matchOrientation);
    }

    public virtual void SetCameraPose(Vector3? localPosition = null, Quaternion? rotation = null)
    {
        //VR模式由设备控制相机，所以可以直接忽略
    }
    #endregion

    #region Attach and follow Target
    /// <summary>
    /// 需要确保目标有效（避免因为重置或切换场景导致目标丢失）
    /// </summary>
    public virtual bool IsRigAttaching { get { return tfCurAttachTarget != null; } }
    //public ParentConstraint parentConstraintRigParent;//【Bug】：ParentConstraint不是即时计算的，改用LateUpdate自行更新！
    protected Transform tfCurAttachTarget;//当前附着的物体
    public void TeleportAndAttachTo(AD_RigAttachable rigAttachable)
    {
        //#0 主动保存Pose信息，可避免间隔保存导致滞后
        if (!tfCurAttachTarget)//避免在不同Attachable间切换，导致保存错误的信息
            SaveRigPose();

        //#1 初始化Locomotion设定，临时设置为无重力模式
        SetMovementTypeFunc(true, true, false);

        //#2 使用Parent Constraints来将tfCameraRigParent附着到目标
        Transform attachTransform = rigAttachable.attachTransform;
        tfCameraRigParent.SetPositionAndRotation(attachTransform.position, attachTransform.rotation);

        //#3 把Rig传送到AD_XR Interaction Setup的原点
        TeleportTo(tfCameraRigParent.position, tfCameraRigParent.rotation, MatchOrientation.TargetUpAndForward, rigAttachable.destinationRigPart);

        //Cache
        tfCurAttachTarget = rigAttachable.attachTransform;

        SetAttachState(true);
    }

    protected bool IsAttachingTo(AD_RigAttachable rigAttachable)
    {
        if (rigAttachable == null || tfCurAttachTarget == null)
            return false;
        return rigAttachable.attachTransform == tfCurAttachTarget;
    }

    protected virtual Quaternion CameraRotation { get { return tfCameraEye.rotation; } }

    //缓存有效的位置信息，用于备份还原或Detach
    public Pose PoseCameraRig { get { return poseCameraRig; } }
    public Pose PoseLocalCameraEye { get { return poseLocalCameraEye; } }
    public Pose PoseCameraEye { get { return poseCameraEye; } }
    Pose poseCameraRig;
    Pose poseLocalCameraEye;
    Pose poseCameraEye;

    float savePoseInfoFrequence = 1;//保存位置信息的频率（秒）
    float lastSavePostInfoTime;
    protected virtual void LateUpdate()
    {
        if (tfCurAttachTarget)//Attaching中：让XRRig跟随目标。（因为此时CameraRigParent灯物体的位置有变化，所以不应该保存其信息）
        {
            tfCameraRigParent.position = tfCurAttachTarget.position;
            tfCameraRigParent.rotation = tfCurAttachTarget.rotation;
        }
        else//非Attaching：保存当前有效的位置信息(ToUpdate：降低更新频次，如1秒1次)
        {
            if (Time.time - lastSavePostInfoTime < savePoseInfoFrequence)
                return;
            SaveRigPose();
        }
    }

    void SaveRigPose()
    {
        poseCameraRig = new Pose(tfCameraRig.position, tfCameraRig.rotation);
        poseLocalCameraEye = new Pose(tfCameraEye.localPosition, tfCameraEye.localRotation);
        poseCameraEye = new Pose(tfCameraEye.position, CameraRotation);
        lastSavePostInfoTime = Time.time;
    }

    bool IsTeleportDone
    {
        get
        {
            var locomotionPhase = XRITool.teleportationProvider.locomotionPhase;

            return locomotionPhase == LocomotionPhase.Idle || locomotionPhase == LocomotionPhase.Done;//Idle也算是完成的状态
        }
    }

    protected void TryDetach()
    {
        if (IsRigAttaching)
            Detach();
    }

    static bool IsVRMode { get { return AD_ManagerHolderManager.ActivePlatformMode == AD_PlatformMode.PCVR; } }
    protected virtual void Detach()
    {
        //#0 
        //Pose rigPose = new Pose(tfCameraRig.position, tfCameraRig.rotation);//缓存Rig的Pose，用于后续还原
        Pose rigPose = poseCameraRig;//改为使用Attach前的有效数据，能够避免旋转的Bug

        //#2 还原RigParent的位置/旋转
        ResetRigParentPose();

        //#3 恢复Mod的Locomotion设定
        ActiveController.UpdateLocomotionSetting();

        //#3 还原Rig的位置/旋转，避免出现与Attach时不一致的瞬移
        TeleportTo(rigPose.position, rigPose.rotation, MatchOrientation.TargetUpAndForward, AD_XRDestinationRigPart.Foot);

        //#4 非【VR模式】：还原相机
        if (!IsVRMode)
            SetCameraPose(poseLocalCameraEye.position, poseCameraEye.rotation);

        //Clear Data
        tfCurAttachTarget = null;
        SetAttachState(false);
    }

    public virtual void ResetRigPose()
    {
        TryDetach();//先取消附着（Bug：因为TeleportRequest要在Update中进行更新，此时Controller.OnModControllerDeinit保存的位置会报错。解决办法：应该是保存Attach之前的位置（可以自行保存），而不是当前的位置信息）

        ResetRigParentPose();
        ActiveController.ResetRigPose();

        //重新激活Locomotion，避免因为驾驶车辆出错导致无法移动
        SetLocomotion(true);
    }

    protected virtual void SetAttachState(bool isAttaching)
    {
        //此方法暂不实现但不能删除，子类需要用于更新UI等
    }

    /// <summary>
    /// //重置RigParent位置及朝向，可避免因为Attach到墙上物体导致相机偏转出错的Bug
    /// </summary>
    void ResetRigParentPose()
    {
        tfCameraRigParent.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
    #endregion

    #region Init
    /// <summary>
    /// 在程序初始化时调用
    /// </summary>
    protected void Init()
    {
        InitPlatform();
    }
    /// <summary>
    /// 根据平台类型进行初始化
    /// 
    /// PS:暂时只能在启动时设置
    /// Todo：
    /// 【V2】后期查找如何在运行时切换模式（如缓存InputSystem.devices中所有的XRHMD）
    /// </summary>
    /// <param name="platformMode"></param>
    protected abstract void InitPlatform();


    private void OnApplicationQuit()
    {
        DeinitPlatform();
    }

    void DeinitPlatform()
    {
        //VR模式退出时要Stop，否则再次进入会卡住
        if (AD_ManagerHolderManager.ActivePlatformMode == AD_PlatformMode.PCVR)
            StopXR();
    }

    #region Utility
    bool hasManualStartXR = false;

    protected void StartXR()
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

    protected void StopXR()
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

    #region IHubManagerModInitHandler
    public virtual void OnModInit(Scene scene, ModEntry modEntry)
    {
        //Reset
        SetLocomotion(true);

        modController = scene.GetComponents<IAD_XRController>().FirstOrDefault();
        defaultController.gameObject.SetActive(modController == null);

        ActiveController.OnModControllerInit();//初始化
        ManagerHolderManager.Instance.FireGlobalControllerConfigStateEvent<IAD_SOXRControllerConfig>(modController == null);//设置对应的全局Config是否可用
    }

    public virtual void OnModDeinit(Scene scene, ModEntry modEntry)
    {
        modController?.OnModControllerDeinit();//仅Deinit Mod的Controller
        modController = null;
    }
    #endregion

    #region Controller Callback
    protected virtual void OnPersistentChanged(PersistentChangeState persistentChangeState)
    {
    }
    #endregion
}