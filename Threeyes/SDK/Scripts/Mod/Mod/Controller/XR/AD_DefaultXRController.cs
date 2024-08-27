using Newtonsoft.Json;
using System;
using Threeyes.Config;
using Threeyes.Persistent;
using Threeyes.GameFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using NaughtyAttributes;
using Threeyes.RuntimeEditor;
/// <summary>
/// Control XR Setting and Behaviour
/// 
/// Todo：
/// -暴露常见参数给用户编辑（高度）
/// -可调用某个方法，如ResetPosToSpawnPoint（通过Naughtattribute的Button生成对应UIField，需要专门的Attribute）
/// 
/// -【V3】支持自定义XRController（包括控制器等），将AD_XRGlobalManager大部分成员移动到AD_XRController中，需要提供兼容AD_XR Device Simulator的所有接口，或者是Simulator也由Modder提供（比较复杂，因为涉及跨平台）（需要等VR模式稳定后再开放）
/// 
/// PS：
/// -该SOConfig存储在Item目录下，由Modder决定是否将Config持久化并暴露给用户编辑（只有持久化， 才能保存Rig的信息）
/// </summary>
[AddComponentMenu(AD_EditorDefinition.ComponentMenuPrefix_Root_Mod_Controller + "AD_DefaultXRController")]
public class AD_DefaultXRController : AD_XRControllerBase<AD_SODefaultXRControllerConfig, AD_DefaultXRController.ConfigInfo>
{
    [Required]
    public AD_TeleportationAnchor spawnPoint;//首次进入Mod后默认的生成点，如果再次进入则使用缓存位置。后期可以通过UI重置（参考DP，使用继承实现自定义传送点。可以先不实现传送后隐藏传送点等复杂功能）（还可提供朝向、头等信息）(需要使用Gizmo进行绘制)(可以选择在使用后隐藏，避免影响环境布置)

    ///ToAdd：
    /// -附着点（可选。通过ParentConstraints或类似非父子关系实现。在普通模式可作为其父物体，实现车辆跟随等特殊功能（并且CameraMovementManager会失效）；在编辑模式会临时脱离，由CameraMovementManager控制）

    #region Callback
    static bool IsVRMode { get { return AD_ManagerHolderManager.ActivePlatformMode == AD_PlatformMode.PCVR; } }
    public override void OnModControllerInit()
    {
        Pose exitRigPose = Config.exitRigPose;
        if (Config.isRestoreExitPose && exitRigPose != Pose.identity)//恢复到退出前的姿势
        {
            //ToUpdate:因为TeleportTo在Update中更新，所以建议等待其传送完成后再设置相机位置（可以是通过回调）（或者是SetCameraPose需要等待TeleportTo结束后才完成）
            MatchOrientation matchOrientation = IsVRMode ? MatchOrientation.TargetUpAndForward : MatchOrientation.None;//PS:None可以防止传送时更改相机朝向
            AD_ManagerHolder.XRManager.TeleportTo(exitRigPose.position, exitRigPose.rotation, matchOrientation,
                endLocomotion:
                (lS) =>
                {
                    if (!IsVRMode)//非【VR模式】才更改相机
                        AD_ManagerHolder.XRManager.SetCameraPose(Config.exitCameraPose.position, Config.exitCameraPose.rotation);
                });
        }
        else if (spawnPoint)//传送到初始位置
        {
            spawnPoint.Teleport();//PS:传送后不强制隐藏传送点，而是让Modder通过AD_TeleportationAnchor.isHideOnEnter自由设置，方便重用传送点
        }

        //不管是否传入该传送点都要隐藏。如果Modder希望提供相同位置的传送点，可以在该位置放一个额外的传送点
        if (spawnPoint)
            spawnPoint.Hide();

        UpdateSetting();
    }

    public override void OnModControllerDeinit()
    {
        ///保存退出前的姿势
        if (Config.isRestoreExitPose)
        {
            Config.exitRigPose = AD_ManagerHolder.XRManager.PoseCameraRig;
            if (!IsVRMode)//非【VR模式】才存储相机旋转，因为【VR模式】由头显控制旋转
            {
                Config.exitCameraPose = new Pose(AD_ManagerHolder.XRManager.PoseLocalCameraEye.position, AD_ManagerHolder.XRManager.PoseCameraEye.rotation);//PS：如果是退出前通过tfCamera.rotation获取值，会因为AD_XRDeviceSimulator.OnDisable-RemoveDevices而导致相机提前恢复到默认旋转值，从而返回默认值。因此要明确获取其缓存的旋转值。另外要注意，【编辑器模式】下，需要聚焦Game窗口才会恢复位置，如果不方便，后期可以忽略此设置，改为运行时才恢复
            }
        }
    }

    public override void ResetRigPose()
    {
        //PS:放在这里而不是Manager中，是方便其他自定义的重置选项
        //传送到初始位置
        if (spawnPoint)
            spawnPoint.Teleport();

        if (!IsVRMode)//非【VR模式】才更改相机
            AD_ManagerHolder.XRManager.SetCameraPose(rotation: Quaternion.identity);//需要调用此方法才可重置相机

    }
    protected override void UpdateSetting()
    {
        UpdateLocomotionSetting();
    }
    public override void UpdateLocomotionSetting()
    {
        AD_ManagerHolder.XRManager.SetMovementType(Config.isEnableFly, Config.isPenetrateOnFly, Config.isUseGravity);
    }

    #endregion

    #region Define
    /// <summary>
    ///
    /// ToAdd:
    /// -AttachObj(不更改层级，仅用于【普通模式】时车辆行摄等跟随)（需要提炼到通用父类接口中，方便Manger获取）
    /// </summary>
    [Serializable]
    public class ConfigInfo : AD_XRControllerConfigInfoBase
    {

        ///管理相机的以下设置：
        ///     -使用重力（普通模式）
        ///     -Pose（Pos/Rot）（用于缓存上次的位置，退出时保存，可用额外bool选择是否保存。）（每个Mod应该强制由一个XRController，否则报错）（PS：因为是存在Item而不是Item_Local文件夹，所以不适合使用PB保存）（需要标记为RuntimeEdit不可编辑，只是用于保存数据）
        ///     
        ///以下属性仅在运行时临时有效，不保存到PD中
        ///     -Projection（正交、透视）
        ///     -高度（非必要，因为VR模式按照人高度来动态调整Camera，而不是直接更改rig）
        ///     -FOV（仅PC模式，VR模式无效）
        ///     
        ///其他：
        ///     - 进入编辑前可以 自动或点击按钮 记录相机的最佳观看位置，编辑完成后调用其特定方法恢复原位

        [Tooltip("Controls whether to enable flying (unconstrained movement). This overrides the use of gravity.")]
        [AllowNesting] public bool isEnableFly = true;//【普通模式】飞行模式
        [Tooltip("Controls whether to penetrate during flying")]
        [AllowNesting] [ShowIf(nameof(isEnableFly))] public bool isPenetrateOnFly = true;
        [Tooltip("Controls whether gravity affects this provider when a Character Controller is used and flying is disabled.")]
        [AllowNesting] [DisableIf(nameof(isEnableFly))] public bool isUseGravity = false;//【普通模式】使用重力

        ////——缓存上次的信息——
        [Tooltip("Restore exit pose when re-entering Mod scene.")]
        public bool isRestoreExitPose = false;//缓存退出前的信息
        [RuntimeEditorIgnore] public Pose exitRigPose = Pose.identity;//退出前的Rig信息，其中旋转可能是因为传入了DP（PS：因为VR模式的Camera由用户控制，且体验人的高度不一定一致，所以不保存Camera的Pose并反推出Rig的位置）
        [RuntimeEditorIgnore] public Pose exitCameraPose = Pose.identity;//【PC模式】退出前的相机的信息（局部位置，全局旋转）

        [JsonConstructor]
        public ConfigInfo()
        {
        }
    }
    #endregion

    #region Editor Method
#if UNITY_EDITOR
    //——MenuItem——
    static string instName = "DefaultXRController";
    [UnityEditor.MenuItem(AD_EditorDefinition.HierarchyMenuPrefix_Root_Mod_Controller_XR + "Default", false)]
    public static void CreateInst()
    {
        Threeyes.Core.Editor.EditorTool.CreateGameObjectAsChild<AD_DefaultXRController>(instName);
    }
#endif
    #endregion
}