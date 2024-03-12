using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
///（Rotate Around local X Axis, provding bool switching like Toggle）An interactable lever that snaps into an on or off position by a direct interactor
/// 拉杆（绕局部X轴顺时针旋转），用最大/最小值代表开关，松手后可选是否还原到默认值
/// 
/// PS：
/// -该组件只能提供最大/最小值状态，如果需要旋转到指定角度的事件回调，可以改为使用 AD_XRHinge
/// 
/// Ref:UnityEngine.XR.Content.Interaction.XRLever
/// </summary>
public class AD_XRLever : AD_XRProgressInteractable<AD_XRLever, AD_XRLever.ConfigInfo, AD_XRLever.PropertyBag, bool>
{
    #region Property & Field
    const float k_LeverDeadZone = 0.1f; // Prevents rapid switching between on and off states when right in the middle

    [SerializeField]
    [Tooltip("Events to trigger when the lever activates")]
    UnityEvent onLeverActive = new UnityEvent();

    [SerializeField]
    [Tooltip("Events to trigger when the lever deactivates")]
    UnityEvent onLeverDeactive = new UnityEvent();
    
    [SerializeField]
    [Tooltip("Events to trigger when the lever activate/deactivate")]
    BoolEvent onLeverActiveDeactive = new BoolEvent();

    #endregion

    #region Interaction
    protected override void EndGrabFunc(SelectExitEventArgs args)
    {
        base.EndGrabFunc(args);

        if (Config.lockToValue)
        {
            SetHandleAngle(Config.Value ? Config.maxAngle : Config.minAngle);//直接切换到最大/最小值
        }
    }
    protected override void ProcessInteractableFunc()
    {
        var lookDirection = GetLookDirection();
        var lookAngle = Mathf.Atan2(lookDirection.z, lookDirection.y) * Mathf.Rad2Deg;

        //允许最大/最小角度反转
        if (Config.minAngle < Config.maxAngle)
            lookAngle = Mathf.Clamp(lookAngle, Config.minAngle, Config.maxAngle);
        else
            lookAngle = Mathf.Clamp(lookAngle, Config.maxAngle, Config.minAngle);

        //计算当前角度与最大/最小角度的差值，找出最靠近的端点
        var maxAngleDistance = Mathf.Abs(Config.maxAngle - lookAngle);
        var minAngleDistance = Mathf.Abs(Config.minAngle - lookAngle);

        //根据缓存值，计算出当前是否为新值
        if (Config.Value)
            maxAngleDistance *= (1.0f - k_LeverDeadZone);
        else
            minAngleDistance *= (1.0f - k_LeverDeadZone);

        var newValue = (maxAngleDistance < minAngleDistance);
        SetHandleAngle(lookAngle);

        SetModel(newValue);
        if (Config.Value != newValue)//仅在值变化时才更新事件
        {
            NotifyEvent(newValue);
            Config.Value = newValue;
        }
    }

    void SetHandleAngle(float angle)
    {
        if (tfHandle == null)
            return;

        tfHandle.localRotation = Quaternion.Euler(angle, 0.0f, 0.0f);
    }

    Vector3 GetLookDirection()
    {
        Vector3 direction = cacheInteractor.GetAttachTransform(this).position - tfHandle.position;
        direction = tfHandleParent.InverseTransformDirection(direction);
        direction.x = 0;

        return direction.normalized;
    }
    protected override void SetModel(bool modelValue, bool ignoreRigidbody = false)
    {
        ///PS:
        ///-因为m_Handle由VR操控，其角度一定是最大/最小值，所以不使用该方法直接修改m_Handle
    }
    protected override void NotifyEvent(bool value)
    {
        if (value)
        {
            onLeverActive.Invoke();
        }
        else
        {
            onLeverDeactive.Invoke();
        }
        onLeverActiveDeactive.Invoke(value);
    }
    #endregion

    #region Editor
    protected override void OnDrawGizmosSelected()
    {
        var angleStartPoint = transform.position;

        if (tfHandle != null)
            angleStartPoint = tfHandle.position;

        const float k_AngleLength = 0.25f;

        var angleMaxPoint = angleStartPoint + transform.TransformDirection(Quaternion.Euler(Config.maxAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;
        var angleMinPoint = angleStartPoint + transform.TransformDirection(Quaternion.Euler(Config.minAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(angleStartPoint, angleMaxPoint);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(angleStartPoint, angleMinPoint);
    }
    protected override void OnValidate()
    {
        base.OnValidate();

        //【运行/非运行模式】修改Inspector中的值：同步位置
        //SetModel(Config.Value);
        SetHandleAngle(Config.Value ? Config.maxAngle : Config.minAngle);//直接切换到最大/最小值
        NotifyEvent(Config.Value);
    }
    #endregion

    #region Define
    [Serializable]
    public class ConfigInfo : ProgressInteractableConfigInfo<bool>
    {
        ///ToAdd:
        ///-后期如果有需要，可以缓存Lever的角度

        public override bool Value { get { return value; } set { this.value = value; } }

        [Tooltip("The value of the lever")] public bool value = false;

        [SerializeField]
        [Tooltip("If enabled, the lever will snap to the value position when released")]
        public bool lockToValue;

        [SerializeField]
        [Tooltip("Angle of the lever in the 'off' position")]
        [Range(-180.0f, 180.0f)]
        public float minAngle = -90.0f;

        [SerializeField]
        [Tooltip("Angle of the lever in the 'on' position")]
        [Range(-180.0f, 180.0f)]
        public float maxAngle = 90.0f;
    }

    public class PropertyBag : ConfigurableComponentPropertyBagBase<AD_XRLever, ConfigInfo>
    {
    }
    #endregion
}