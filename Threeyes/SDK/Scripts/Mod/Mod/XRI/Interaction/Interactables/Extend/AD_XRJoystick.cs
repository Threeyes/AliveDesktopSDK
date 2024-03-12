using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// （Rotate around XZ Axis）An interactable joystick that can move side to side, and forward and back by a direct interactor
/// 摇杆(绕局部XZ轴旋转)，支持锁定某个轴，支持固定到某个区间
/// 
///【Bug】：MaxAngle大于45时SetModel的【m_Handle.up】行会报错（Assertion failed on expression: 'CompareApproximately(det, 1.0F, .005f)'），可能是Vector3某个值为NaN
///-解决办法：直接改为更改旋转值，也方便通过Rigidbody进行设置
/// 
/// ToUpdate：
/// -在Config中增加一个字段，专门记录摇杆的旋转值，便于还原时重置
/// -针对不倒翁等无固定锚点：改为值偏转
/// </summary>
public class AD_XRJoystick : AD_XRProgressInteractable<AD_XRJoystick, AD_XRJoystick.ConfigInfo, AD_XRJoystick.PropertyBag, Vector2>
{
    #region Property & Field
    [SerializeField]
    [Tooltip("Events to trigger when the joystick's x value changes")]
    FloatEvent onValueChangedX = new FloatEvent();

    [SerializeField]
    [Tooltip("Events to trigger when the joystick's y value changes")]
    FloatEvent onValueChangedY = new FloatEvent();

    [SerializeField]
    [Tooltip("Events to trigger when the joystick's value changes")]
    Vector2Event onValueChanged = new Vector2Event();
    #endregion

    #region Interaction
    protected override void EndGrabFunc(SelectExitEventArgs args)
    {
        if (Config.recenterOnRelease)//取消抓取后重新居中
        {
            Config.Value = Vector2.zero;

            SetModel(Vector2.zero);
            NotifyEvent(Vector2.zero);
        }

        base.EndGrabFunc(args);
    }

    protected override void ProcessInteractableFunc()
    {
        var grabPointDirection = GetGrabPointDirection();

        // Get up/down angle and left/right angle
        var upDownAngle = Mathf.Atan2(grabPointDirection.z, grabPointDirection.y) * Mathf.Rad2Deg;
        var leftRightAngle = Mathf.Atan2(grabPointDirection.x, grabPointDirection.y) * Mathf.Rad2Deg;

        // Extract signs
        var signX = Mathf.Sign(leftRightAngle);
        var signY = Mathf.Sign(upDownAngle);

        upDownAngle = Mathf.Abs(upDownAngle);
        leftRightAngle = Mathf.Abs(leftRightAngle);

        var stickValue = new Vector2(leftRightAngle, upDownAngle) * (1.0f / Config.maxAngle);

        // Clamp the stick value between 0 and 1 when doing everything but circular stick motion
        if (Config.joystickMotion == JoystickType.BothCircle)
        {
            // With circular motion, if the stick value is greater than 1, we normalize
            // This way, an extremely strong value in one direction will influence the overall stick direction
            if (stickValue.magnitude > 1.0f)//确保BothCircle模式时，向量是球形以便在后续的SetModel正确呈现
            {
                stickValue.Normalize();
            }
        }
        else//非BothCircle：对移动范围进行裁剪以变为方形活动范围
        {
            stickValue.x = Mathf.Clamp01(stickValue.x);
            stickValue.y = Mathf.Clamp01(stickValue.y);
        }

        //Debug.LogError("ToDelete: " + grabPointDirection + "_" + stickValue);
        // Rebuild the angle values for visuals
        leftRightAngle = stickValue.x * signX * Config.maxAngle;
        upDownAngle = stickValue.y * signY * Config.maxAngle;

        // Apply deadzone and sign back to the logical stick value
        //var deadZone = m_DeadZoneAngle / m_MaxAngle;
        var aliveZone = (1.0f - Config.deadZone);
        stickValue.x = Mathf.Clamp01((stickValue.x - Config.deadZone)) / aliveZone;
        stickValue.y = Mathf.Clamp01((stickValue.y - Config.deadZone)) / aliveZone;

        // Re-apply signs
        stickValue.x *= signX;
        stickValue.y *= signY;

        //#2 更新Config中的数据
        Config.Value = stickValue;

        //#3 通知模型及事件更新
        SetModel_Angle(new Vector2(leftRightAngle, upDownAngle));//传入的是处理过的角度（基于JoystickType进行裁剪）
        NotifyEvent(stickValue);
    }

    /// <summary>
    /// 计算Handle的局部坐标中心到Interactor的矢量，从而计算抓取点的朝向
    /// </summary>
    /// <returns></returns>
    Vector3 GetGrabPointDirection()
    {
        //原版实现：因为m_Handle的位置是固定的，通过计算Interactor与m_Handle的相对值从而计算出偏转值，会导致模型跳到指定位置。不算bug，暂时不需要使用下方【类似AD_XRSlider计算相对值】的代码
        //Vector3 direction = InteractorAttachTransform.position - m_Handle.position;
        //direction = transform.InverseTransformDirection(direction);//转为局部坐标

        //#自定义实现1：基于抓取的相对位移计算朝向
        //var curGrabLocalPosition = GetGrabLocalPosition();
        //Vector3 direction = curGrabLocalPosition - lastGrabLocalPos;//计算与上次抓取的位移值
        //lastGrabLocalPos = curGrabLocalPosition;

        //#自定义实现2：基于父物体坐标进行计算
        Vector3 curGrabLocalPosition = GetGrabLocalPosition();
        if (curGrabLocalPosition.y < 0) //如果Interactor的抓取点移动到Handle的反方向（常见于通过WSAD进行远程移动），那么就会导致Handle被锁定。此时只需要对其位置的Y轴进行反转，确保其在局部坐标的Y轴正面
            curGrabLocalPosition.y = -curGrabLocalPosition.y;
        Vector3 direction = curGrabLocalPosition;//局部坐标中，位置就是其朝向


        switch (Config.joystickMotion)
        {
            case JoystickType.FrontBack:
                direction.x = 0;
                break;
            case JoystickType.LeftRight:
                direction.z = 0;
                break;
        }

        direction.y = Mathf.Clamp(direction.y, 0.01f, 1.0f);
        return direction.normalized;
    }

    protected override void SetModel(Vector2 modelValue, bool ignoreRigidbody = false)
    {
        if (tfHandle == null)
            return;

        ////PS：传入的modelValue是处理后的角度【leftRightAngle, upDownAngle】而不是归一化值

        /////【原版实现】Bug:maxAngle超过45度会报错
        //var xComp = Mathf.Tan(modelValue.x * Mathf.Deg2Rad);
        //var zComp = Mathf.Tan(modelValue.y * Mathf.Deg2Rad);
        //var largerComp = Mathf.Max(Mathf.Abs(xComp), Mathf.Abs(zComp));
        //var yComp = Mathf.Sqrt(1.0f - largerComp * largerComp);//通过三角函数计算出第三个轴
        //Debug.LogWarning($"Debug: modelValue：{modelValue}_{xComp}_{yComp}_{zComp} => {(transform.up * yComp)} + {(transform.right * xComp)} + {(transform.forward * zComp)}");
        //m_Handle.up = (transform.up * yComp) + (transform.right * xComp) + (transform.forward * zComp);

        SetModel_Angle(modelValue * Config.maxAngle, ignoreRigidbody);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="modelValue">值为角度</param>
    /// <param name="ignoreRigidbody"></param>
    void SetModel_Angle(Vector2 modelValue, bool ignoreRigidbody = false)
    {
        if (tfHandle == null)
            return;

        ////PS：传入的modelValue是处理后的角度【leftRightAngle, upDownAngle】而不是归一化值

        /////【原版实现】Bug:maxAngle超过45度会报错
        //var xComp = Mathf.Tan(modelValue.x * Mathf.Deg2Rad);
        //var zComp = Mathf.Tan(modelValue.y * Mathf.Deg2Rad);
        //var largerComp = Mathf.Max(Mathf.Abs(xComp), Mathf.Abs(zComp));
        //var yComp = Mathf.Sqrt(1.0f - largerComp * largerComp);//通过三角函数计算出第三个轴
        //Debug.LogWarning($"Debug: modelValue：{modelValue}_{xComp}_{yComp}_{zComp} => {(transform.up * yComp)} + {(transform.right * xComp)} + {(transform.forward * zComp)}");
        //m_Handle.up = (transform.up * yComp) + (transform.right * xComp) + (transform.forward * zComp);

        tfHandle.localEulerAngles = new Vector3(modelValue.y, 0, -modelValue.x);

        //ToUpdate:改为支持Rigidbody
    }

    protected override void NotifyEvent(Vector2 value)
    {
        onValueChangedX.Invoke(value.x);
        onValueChangedY.Invoke(value.y);
        onValueChanged.Invoke(value);
    }

    #endregion

    #region Editor
    protected override void OnDrawGizmosSelected()
    {
        var angleStartPoint = transform.position;

        if (tfHandle != null)
            angleStartPoint = tfHandle.position;

        const float k_AngleLength = 0.25f;

        if (Config.joystickMotion != JoystickType.LeftRight)
        {
            Gizmos.color = Color.green;
            var axisPoint1 = angleStartPoint + transform.TransformDirection(Quaternion.Euler(Config.maxAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;
            var axisPoint2 = angleStartPoint + transform.TransformDirection(Quaternion.Euler(-Config.maxAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;
            Gizmos.DrawLine(angleStartPoint, axisPoint1);
            Gizmos.DrawLine(angleStartPoint, axisPoint2);

            if (Config.DeadZoneAngle > 0.0f)
            {
                Gizmos.color = Color.red;
                axisPoint1 = angleStartPoint + transform.TransformDirection(Quaternion.Euler(Config.DeadZoneAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;
                axisPoint2 = angleStartPoint + transform.TransformDirection(Quaternion.Euler(-Config.DeadZoneAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;
                Gizmos.DrawLine(angleStartPoint, axisPoint1);
                Gizmos.DrawLine(angleStartPoint, axisPoint2);
            }
        }

        if (Config.joystickMotion != JoystickType.FrontBack)
        {
            Gizmos.color = Color.green;
            var axisPoint1 = angleStartPoint + transform.TransformDirection(Quaternion.Euler(0.0f, 0.0f, Config.maxAngle) * Vector3.up) * k_AngleLength;
            var axisPoint2 = angleStartPoint + transform.TransformDirection(Quaternion.Euler(0.0f, 0.0f, -Config.maxAngle) * Vector3.up) * k_AngleLength;
            Gizmos.DrawLine(angleStartPoint, axisPoint1);
            Gizmos.DrawLine(angleStartPoint, axisPoint2);

            if (Config.DeadZoneAngle > 0.0f)
            {
                Gizmos.color = Color.red;
                axisPoint1 = angleStartPoint + transform.TransformDirection(Quaternion.Euler(0.0f, 0.0f, Config.DeadZoneAngle) * Vector3.up) * k_AngleLength;
                axisPoint2 = angleStartPoint + transform.TransformDirection(Quaternion.Euler(0.0f, 0.0f, -Config.DeadZoneAngle) * Vector3.up) * k_AngleLength;
                Gizmos.DrawLine(angleStartPoint, axisPoint1);
                Gizmos.DrawLine(angleStartPoint, axisPoint2);
            }
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (!Application.isPlaying)
        {
            Vector2 value = Config.value;
            value.x = Mathf.Clamp01(value.x);
            value.y = Mathf.Clamp01(value.y);
            Config.value = value;
        }
        //【运行/非运行模式】修改Inspector中的值：同步位置
        SetModel(Config.Value);//ToUpdate：此时参数未处理，应该参考ProcessInteractableFunc的实现进行处理
        NotifyEvent(Config.Value);
    }
    #endregion

    #region Define
    [Serializable]
    public class ConfigInfo : ProgressInteractableConfigInfo<Vector2>
    {
        public override Vector2 Value { get { return value; } set { this.value = value; } }

        [Tooltip("The value of the joystick")]
        public Vector2 value = Vector2.zero;//ToUpdate: 新增Vector2Range的runtimeAttribute

        [Tooltip("Controls how the joystick moves")]
        public JoystickType joystickMotion = JoystickType.BothCircle;


        [Tooltip("If true, the joystick will return to center on release")]
        public bool recenterOnRelease = true;

        [Tooltip("Maximum angle the joystick can move")]
        [Range(1.0f, 89f)]//Warning：现有算法超过90度会出错，且Joystick不需要如此大的角度，因此限定为89度
        public float maxAngle = 40.0f;

        //[SerializeField]
        public float DeadZoneAngle { get { return maxAngle * deadZone; } }
        [Tooltip("Minimum amount the joystick must move off the center to register changes")]
        [Range(0, 0.9f)] public float deadZone = 0.1f;//（如果仅用于控制旋转，可以设置为0）
    }

    public class PropertyBag : ConfigurableComponentPropertyBagBase<AD_XRJoystick, ConfigInfo>
    {
    }

    public enum JoystickType
    {
        BothCircle,//将移动范围改为中心的圆环内
        BothSquare,
        FrontBack,
        LeftRight,
    }
    #endregion
}
