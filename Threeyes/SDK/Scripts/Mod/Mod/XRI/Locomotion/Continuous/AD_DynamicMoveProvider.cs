using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
///  A version of action-based continuous movement that automatically controls the frame of reference that
/// determines the forward direction of movement based on user preference for each hand.
/// For example, can configure to use head relative movement for the left hand and controller relative movement for the right hand.
/// 
/// Ref:UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider
/// 
/// 功能：
/// -【编辑模式】移动时忽略TimeScale对deltaTime的影响（主要方法：ComputeDesiredMove、MoveRig）
/// </summary>
public class AD_DynamicMoveProvider : ActionBasedContinuousMoveProvider
    , IAD_RuntimeEditor_ModeActiveHandler

{
    /// <summary>
    /// Defines which transform the XR Origin's movement direction is relative to.
    /// </summary>
    /// <seealso cref="leftHandMovementDirection"/>
    /// <seealso cref="rightHandMovementDirection"/>
    public enum MovementDirection
    {
        /// <summary>
        /// Use the forward direction of the head (camera) as the forward direction of the XR Origin's movement.
        /// </summary>
        HeadRelative,

        /// <summary>
        /// Use the forward direction of the hand (controller) as the forward direction of the XR Origin's movement.
        /// </summary>
        HandRelative,
    }

    [Space, Header("Movement Direction")]
    [SerializeField]
    [Tooltip("Directs the XR Origin's movement when using the head-relative mode. If not set, will automatically find and use the XR Origin Camera.")]
    Transform m_HeadTransform;

    /// <summary>
    /// Directs the XR Origin's movement when using the head-relative mode. If not set, will automatically find and use the XR Origin Camera.
    /// </summary>
    public Transform headTransform
    {
        get => m_HeadTransform;
        set => m_HeadTransform = value;
    }

    [SerializeField]
    [Tooltip("Directs the XR Origin's movement when using the hand-relative mode with the left hand.")]
    Transform m_LeftControllerTransform;

    /// <summary>
    /// Directs the XR Origin's movement when using the hand-relative mode with the left hand.
    /// </summary>
    public Transform leftControllerTransform
    {
        get => m_LeftControllerTransform;
        set => m_LeftControllerTransform = value;
    }

    [SerializeField]
    [Tooltip("Directs the XR Origin's movement when using the hand-relative mode with the right hand.")]
    Transform m_RightControllerTransform;

    public Transform rightControllerTransform
    {
        get => m_RightControllerTransform;
        set => m_RightControllerTransform = value;
    }

    [SerializeField]
    [Tooltip("Whether to use the specified head transform or left controller transform to direct the XR Origin's movement for the left hand.")]
    MovementDirection m_LeftHandMovementDirection;

    /// <summary>
    /// Whether to use the specified head transform or controller transform to direct the XR Origin's movement for the left hand.
    /// </summary>
    /// <seealso cref="MovementDirection"/>
    public MovementDirection leftHandMovementDirection
    {
        get => m_LeftHandMovementDirection;
        set => m_LeftHandMovementDirection = value;
    }

    [SerializeField]
    [Tooltip("Whether to use the specified head transform or right controller transform to direct the XR Origin's movement for the right hand.")]
    MovementDirection m_RightHandMovementDirection;

    /// <summary>
    /// Whether to use the specified head transform or controller transform to direct the XR Origin's movement for the right hand.
    /// </summary>
    /// <seealso cref="MovementDirection"/>
    public MovementDirection rightHandMovementDirection
    {
        get => m_RightHandMovementDirection;
        set => m_RightHandMovementDirection = value;
    }

    Transform m_CombinedTransform;
    Pose m_LeftMovementPose = Pose.identity;
    Pose m_RightMovementPose = Pose.identity;

    /// <inheritdoc />
    protected override void Awake()
    {
        base.Awake();

        m_CombinedTransform = new GameObject("[Dynamic Move Provider] Combined Forward Source").transform;
        m_CombinedTransform.SetParent(transform, false);
        m_CombinedTransform.localPosition = Vector3.zero;
        m_CombinedTransform.localRotation = Quaternion.identity;

        forwardSource = m_CombinedTransform;
    }

    /// <inheritdoc />
    protected override Vector3 ComputeDesiredMove(Vector2 input)
    {
        // Don't need to do anything if the total input is zero.
        // This is the same check as the base method.
        if (input == Vector2.zero)
            return Vector3.zero;

        // Initialize the Head Transform if necessary, getting the Camera from XR Origin
        if (m_HeadTransform == null)
        {
            var xrOrigin_Ex = system.xrOrigin;
            if (xrOrigin_Ex != null)
            {
                var xrCamera = xrOrigin_Ex.Camera;
                if (xrCamera != null)
                    m_HeadTransform = xrCamera.transform;
            }
        }

        // Get the forward source for the left hand input
        switch (m_LeftHandMovementDirection)
        {
            case MovementDirection.HeadRelative:
                if (m_HeadTransform != null)
                    m_LeftMovementPose = m_HeadTransform.GetWorldPose();

                break;

            case MovementDirection.HandRelative:
                if (m_LeftControllerTransform != null)
                    m_LeftMovementPose = m_LeftControllerTransform.GetWorldPose();

                break;

            default:
                Assert.IsTrue(false, $"Unhandled {nameof(MovementDirection)}={m_LeftHandMovementDirection}");
                break;
        }

        // Get the forward source for the right hand input
        switch (m_RightHandMovementDirection)
        {
            case MovementDirection.HeadRelative:
                if (m_HeadTransform != null)
                    m_RightMovementPose = m_HeadTransform.GetWorldPose();

                break;

            case MovementDirection.HandRelative:
                if (m_RightControllerTransform != null)
                    m_RightMovementPose = m_RightControllerTransform.GetWorldPose();

                break;

            default:
                Assert.IsTrue(false, $"Unhandled {nameof(MovementDirection)}={m_RightHandMovementDirection}");
                break;
        }

        // Combine the two poses into the forward source based on the magnitude of input
        var leftHandValue = leftHandMoveAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        var rightHandValue = rightHandMoveAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

        var totalSqrMagnitude = leftHandValue.sqrMagnitude + rightHandValue.sqrMagnitude;
        var leftHandBlend = 0.5f;
        if (totalSqrMagnitude > Mathf.Epsilon)
            leftHandBlend = leftHandValue.sqrMagnitude / totalSqrMagnitude;

        var combinedPosition = Vector3.Lerp(m_RightMovementPose.position, m_LeftMovementPose.position, leftHandBlend);
        var combinedRotation = Quaternion.Slerp(m_RightMovementPose.rotation, m_LeftMovementPose.rotation, leftHandBlend);
        m_CombinedTransform.SetPositionAndRotation(combinedPosition, combinedRotation);


        //——【修改】忽略TimeScale对deltaTime的影响——
        //return base.ComputeDesiredMove(input);
        //Ref:DynamicMoveProvider.ComputeDesiredMove
        var xrOrigin = system.xrOrigin;
        if (xrOrigin == null)
            return Vector3.zero;

        var inputMove = Vector3.ClampMagnitude(new Vector3(enableStrafe ? input.x : 0f, 0f, input.y), 1f);

        // Determine frame of reference for what the input direction is relative to
        var forwardSourceTransform = forwardSource == null ? xrOrigin.Camera.transform : forwardSource;
        var inputForwardInWorldSpace = forwardSourceTransform.forward;

        var originTransform = xrOrigin.Origin.transform;
        var speedFactor = moveSpeed * Time.unscaledDeltaTime * originTransform.localScale.x; // Adjust speed with user scale【修改为unscaledTime】

        // If flying, just compute move directly from input and forward source
        if (enableFly)
        {
            var inputRightInWorldSpace = forwardSourceTransform.right;
            var combinedMove = inputMove.x * inputRightInWorldSpace + inputMove.z * inputForwardInWorldSpace;
            return combinedMove * speedFactor;
        }

        var originUp = originTransform.up;

        if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(inputForwardInWorldSpace, originUp)), 1f))
        {
            // When the input forward direction is parallel with the rig normal,
            // it will probably feel better for the player to move along the same direction
            // as if they tilted forward or up some rather than moving in the rig forward direction.
            // It also will probably be a better experience to at least move in a direction
            // rather than stopping if the head/controller is oriented such that it is perpendicular with the rig.
            inputForwardInWorldSpace = -forwardSourceTransform.up;
        }

        var inputForwardProjectedInWorldSpace = Vector3.ProjectOnPlane(inputForwardInWorldSpace, originUp);
        var forwardRotation = Quaternion.FromToRotation(originTransform.forward, inputForwardProjectedInWorldSpace);

        var translationInRigSpace = forwardRotation * inputMove * speedFactor;
        var translationInWorldSpace = originTransform.TransformDirection(translationInRigSpace);

        return translationInWorldSpace;
    }

    ///Todo:将MoveRig的deltaTime改为unscaledDeltaTime
    //protected override void MoveRig(Vector3 translationInWorldSpace)
    //{
    //    var xrOrigin = system.xrOrigin?.Origin;
    //    if (xrOrigin == null)
    //        return;

    //    FindCharacterController();

    //    var motion = translationInWorldSpace;

    //    if (m_CharacterController != null && m_CharacterController.enabled)
    //    {
    //        // Step vertical velocity from gravity
    //        if (m_CharacterController.isGrounded || !m_UseGravity || m_EnableFly)
    //        {
    //            m_VerticalVelocity = Vector3.zero;
    //        }
    //        else
    //        {
    //            m_VerticalVelocity += Physics.gravity * Time.deltaTime;
    //        }

    //        motion += m_VerticalVelocity * Time.deltaTime;

    //        if (CanBeginLocomotion() && BeginLocomotion())
    //        {
    //            // Note that calling Move even with Vector3.zero will have an effect by causing isGrounded to update
    //            m_IsMovingXROrigin = true;
    //            m_CharacterController.Move(motion);
    //            EndLocomotion();
    //        }
    //    }
    //    else
    //    {
    //        if (CanBeginLocomotion() && BeginLocomotion())
    //        {
    //            m_IsMovingXROrigin = true;
    //            xrOrigin.transform.position += motion;
    //            EndLocomotion();
    //        }
    //    }
    //}


    //protected new void Update()
    //{
    //      //尝试临时把timeScale恢复。
    //      //【测试结果】：不行，因为deltaTime是基于上一帧进行计算的
    //    float cacheTimeScale = Time.timeScale;
    //    Time.timeScale = 1;
    //    Debug.LogError("deltaTime: " + Time.deltaTime);
    //    base.Update();
    //    Time.timeScale = cacheTimeScale;
    //}

    #region IAD_RuntimeEditor_ModeActiveHandler
    bool isRuntimeEditorActive;
    public void OnRuntimeEditorActiveChanged(bool isRuntimeEditorActive)
    {
        this.isRuntimeEditorActive = isRuntimeEditorActive;
    }
    #endregion

}
