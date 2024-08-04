using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public interface IAD_XRManager :
    IHubManagerWithController<IAD_XRController>
     , IHubManagerModInitHandler
{
    Transform TfCameraRigParent { get; }
    Transform TfCameraRig { get; }
    Transform TfCameraEye { get; }
    Camera VrCamera { get; }
    Transform TfLeftController { get; }
    Transform TfRightController { get; }
    ActionBasedController LeftController { get; }
    ActionBasedController RightController { get; }
    Ray LeftControllerRay { get; }

    /// <summary>
    /// Last valid pose (Ignore attaching state)
    /// </summary>
    Pose PoseCameraRig { get; }
    Pose PoseLocalCameraEye { get; }
    Pose PoseCameraEye { get; }

    bool EnableLocomotion { get; }
    bool EnableFly { get; }
    bool UseGravity { get; }
    bool IsRigAttaching { get; }

    void TeleportAndAttachTo(AD_RigAttachable rigAttachable);

    void RegisterUserInput(IAD_XRUserInput userInput);
    void UnRegisterUserInput(IAD_XRUserInput userInput);

    /// <summary>
    /// Enable/Disable locomotion and turn
    /// 
    /// Use case:
    /// -Disallow locomotion when Driving
    /// </summary>
    /// <param name="isEnable"></param>
    void SetLocomotion(bool isEnable);

    /// <summary>
    /// Set the movement type of the rig
    /// </summary>
    /// <param name="enableFly">Controls whether to enable flying (unconstrained movement). This overrides <see cref="useGravity"/></param>
    /// <param name="isPenetrateOnFly"> Whether penetrate during flying</param>
    /// <param name="useGravity">Controls whether gravity affects this provider when a <see cref="CharacterController"/> is used. This only applies when <see cref="enableFly"/> is <see langword="false"/>.</param>
    void SetMovementType(bool enableFly, bool isPenetrateOnFly, bool useGravity);

    /// <summary>
    /// Teleport Rig to target pos
    /// 
    /// Warning：
    /// -The teleport function is implemented in Update, so it will not take effect immediately(传送功能在Update中实现,因此不会立即生效)
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    void TeleportTo(Vector3 position, Quaternion rotation, MatchOrientation matchOrientation, AD_XRDestinationRigPart destinationRigPart = AD_XRDestinationRigPart.Foot, Action<LocomotionSystem> beginLocomotion = null, Action<LocomotionSystem> endLocomotion = null);

    /// <summary>
    /// (PC Mode only) Set vr camera's position and rotation
    /// </summary>
    /// <param name="localPosition"></param>
    /// <param name="rotation"></param>
    void SetCameraPose(Vector3? localPosition = null, Quaternion? rotation = null);

    void ResetRigPose();
}