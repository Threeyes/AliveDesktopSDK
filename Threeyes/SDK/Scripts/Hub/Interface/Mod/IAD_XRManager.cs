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
    AD_DynamicMoveProvider DynamicMoveProvider { get; }
    /// <summary>
    /// Teleport Rig to target pos&rot
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public void TeleportTo(Vector3 position, Quaternion rotation, MatchOrientation matchOrientation);

    public void ResetPose();
}