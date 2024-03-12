using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using Threeyes.Steamworks;
using UnityEngine;
/// <summary>
/// （Rotate around pivot point）
/// 绕锚点旋转，Y轴为法线轴
/// 
/// Todo：
/// -随意绕轴旋转，暂不限制角度
/// 
/// ToFix:
/// -朝向其他角度的时候，位移方向会出错
/// </summary>
public class AD_XRRotater : AD_XRProgressInteractable<AD_XRRotater, AD_XRRotater.ConfigInfo, AD_XRRotater.PropertyBag, Vector3>
{
    #region Property & Field
    [Tooltip("Events to trigger when the rotater is moved")]
    Vector3Event onValueChanged = new Vector3Event();
    #endregion

    #region Interaction
    protected override void ProcessInteractableFunc()
    {
        //Todo：计算Y轴的朝向，得出目标旋转值
        Vector3 curGrabPosition = GetGrabPosition();

        Vector3 grabPointDirection = curGrabPosition - tfHandle.position;//计算Handle到抓取点的矢量
        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, grabPointDirection);//使模型的Y轴朝向矢量

        Vector3 targetEulerAngle = targetRotation.eulerAngles;

        //#2 更新Config中的数据
        Config.Value = targetEulerAngle;

        //#3 通知模型及事件更新
        SetModel(targetEulerAngle);
        NotifyEvent(targetEulerAngle);
    }

    public float sensitivity = 0.05f;
    public float PivotThickness = 0;//默认缩放时中心的厚度，用于模拟滚动（为0则不生效）
    protected override void SetModel(Vector3 modelValue, bool ignoreRigidbody = false)
    {
        if (tfHandle == null)
            return;

        Quaternion targetRotation = Quaternion.Euler(modelValue);

        Rigidbody rigidbody = tfHandle.GetComponent<Rigidbody>();
        if (Application.isPlaying && !ignoreRigidbody && rigidbody)//【运行模式】如果目标是刚体， 则使用刚体的移动方法，否则可能会导致刚体穿透
        {
            //#模拟因摩擦力引起的位移：增加PivotThickness属性，根据角度差值计算对应的位置差值并传给MovePosition（需要提供Radius字段，用于计算走过的周长）（Warning：仅限于平面，不适用于其他情况，可以用射线测试前方是否有障碍物，或者由物体引擎计算）
            ///Bug：
            ///-移动到平面时容易抖动
            float deltaAngle = Quaternion.Angle(targetRotation, tfHandle.rotation);
            float deltaOffset = (deltaAngle / 360) * 2 * Mathf.PI * PivotThickness * tfHandle.lossyScale.x;//从偏移角度得出偏移位置(需要乘以全局缩放）

            Vector3 moveDirection = targetRotation.ToVector(Vector3.up) - tfHandle.rotation.ToVector(Vector3.up);//通过计算旋转矢量的差值可知道移动方向
            moveDirection.y = 0;//放在XZ平面，得到朝向值（可能有偏转问题）
            //ToDo：根据moveDirection与原点的关系，决定方向
            Vector3 targetPos = tfHandle.position + moveDirection.normalized * deltaOffset;//朝向目标方向移动（因为默认Y轴为原点，所以可以根据角度知道是否需要增加还是减少
            rigidbody.MovePosition(targetPos);
            //Debug.LogError(deltaAngle + "__" + deltaOffset + "__" + moveDirection);

            //#更改旋转值
            rigidbody.MoveRotation(targetRotation);

            ////#实现3：使用 AddRelativeTorque/angularVelocity进行偏转，直到朝向到达一定阈值内(Bug:容易导致飘）
            //var rotation = Quaternion.FromToRotation(tfHandle.forward, modelValue.AngleToVector(Vector3.forward)).eulerAngles * sensitivity;
            //rigidbody.angularVelocity = rotation;
        }
        else//忽略刚体：直接移动，确保能移动到正确位置
        {
            tfHandle.SetProperty(rotation: targetRotation);//该方法能自动使用合适的方法进行移动
        }
    }
    /// <summary>
    /// 计算角度差，可以用其他方法代替
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    private static float GetDiff(float A, float B)
    {
        A = Mathf.Repeat(A + 180, 360) - 180;
        B = Mathf.Repeat(B + 180, 360) - 180;
        return (A - B);
    }
    private static void TorqueLerp(Rigidbody2D rb, float Diff, float Dist)
    {
        float Force = Diff * (Dist / 2);
        rb.angularVelocity = Force;
    }

    protected override void NotifyEvent(Vector3 value)
    {
        onValueChanged.Invoke(value);
    }
    #endregion

    #region Define
    [Serializable]
    public class ConfigInfo : ProgressInteractableConfigInfo<Vector3>
    {
        public override Vector3 Value { get { return value; } set { this.value = value; } }

        [Tooltip("The value of the rotater")]
        public Vector3 value = Vector3.zero;
    }

    public class PropertyBag : ConfigurableComponentPropertyBagBase<AD_XRRotater, ConfigInfo>
    {
    }
    #endregion
}
