using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using Threeyes.Steamworks;
using UnityEngine;
/// <summary>
/// （Rotate around pivot point）
/// 绕锚点旋转，Y轴为旋转时的轴
/// 
/// Todo：
/// +随意绕轴旋转，暂不限制角度，后续有需要再加上
/// +XR的操作改为：X轴位移对应玩家坐标Y轴旋转，Y轴位移对应玩家坐标X轴旋转（类似按X键的效果）（增加sensitivity等参数）
/// 
/// PS：
/// -因为GrabPoint是绕手柄进行转动，所以其位移可能到达临界点后会出现反转，问题不大。
/// ToFix:
/// -朝向其他角度的时候，位移方向会出错
/// </summary>
public class AD_XRRotater : AD_XRProgressInteractable<AD_XRRotater, AD_XRRotater.ConfigInfo, AD_XRRotater.PropertyBag, Vector3>
{
    #region Property & Field
    public Vector2 rotateSensitivity = new Vector2(180, 180);
    [SerializeField]
    [Tooltip("Events to trigger when the rotater is moved")]
    Vector3Event onValueChanged = new Vector3Event();
    #endregion
    //Runtime

    public Transform TfCamera
    {
        get
        {
            if (!tfCamera)
            {
                if (AD_ManagerHolder.XRManager != null)
                {
                    tfCamera = AD_ManagerHolder.XRManager.TfCameraEye;
                }
                else//测试
                {
                    Camera mainCamera = Camera.main;
                    if (mainCamera)
                        tfCamera = Camera.main.transform;
                }
            }
            return tfCamera;
        }
    }
    Transform tfCamera;

    #region Interaction
    protected override void ProcessInteractableFunc()
    {
        Vector3 targetEulerAngle = Vector3.zero;
        ////——实现1：使Y轴对齐grabPointDirection（仅针对Direct Interactor）（经测试可用，后续有需要再激活）——
        // Vector3 curGrabPosition = GetGrabPosition();
        // Vector3 grabPointDirection = curGrabPosition - tfHandle.position;//计算Handle到抓取点的矢量
        // Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, grabPointDirection);//使模型的Y轴朝向矢量，得出目标旋转值
        //targetEulerAngle = targetRotation.eulerAngles;

        //——【实现2：类似PC模式按X键的实现（即通过位移决定旋转）】——
        //计算grabPoint相对于相机坐标的位移
        Vector3 curGrabPosition = GetGrabPosition();
        Vector3 grabOffset = curGrabPosition - lastGrabPos;//计算与上次抓取的位移值
        lastGrabPos = curGrabPosition;
        Vector3 grabOffsetRelatedToCamera = TfCamera.InverseTransformDirection(grabOffset);


        Vector3 frontAxis = tfHandle.position - TfCamera.position;
        Vector3 upAxis = TfCamera.up;
        Vector3 rightAxis = Vector3.Cross(upAxis, frontAxis);//计算出对应的right轴

        Vector3 localUpAxis = tfHandle.InverseTransformDirection(upAxis);//通过父物体计算的局部矢量
        Vector3 localRightAxis = tfHandle.InverseTransformDirection(rightAxis);

        //计算出目标全局旋转值
        Quaternion curRotation = tfHandle.localRotation;
        Quaternion targetRotation = curRotation.RotateAround(localUpAxis, -grabOffsetRelatedToCamera.x * rotateSensitivity.x, Space.Self);
        targetRotation = targetRotation.RotateAround(localRightAxis, grabOffsetRelatedToCamera.y * rotateSensitivity.y, Space.Self);//相对于tfHandle与相机连线的右侧，这样能够不受相机旋转影响

        targetEulerAngle = targetRotation.eulerAngles;
        //Debug.LogError(grabOffsetRelatedToCamera + " " + targetEulerAngle);

        //#2 更新Config中的数据
        Config.Value = targetEulerAngle;

        //#3 通知模型及事件更新
        SetModel(targetEulerAngle);
        NotifyEvent(targetEulerAngle);
    }

    //public float sensitivity = 0.05f;
    //public float PivotThickness = 0;//默认缩放时中心的厚度，用于模拟滚动（为0则不生效）
    /// <summary>
    /// 
    /// </summary>
    /// <param name="modelValue">局部旋转值</param>
    /// <param name="ignoreRigidbody"></param>
    protected override void SetModel(Vector3 modelValue, bool ignoreRigidbody = false)
    {
        if (tfHandle == null)
            return;

        //——【实现1】——
        Quaternion targetRotation = Quaternion.Euler(modelValue);
        Rigidbody rigidbody = tfHandle.GetComponent<Rigidbody>();
        if (Application.isPlaying && !ignoreRigidbody && rigidbody)//【运行模式】如果目标是刚体， 则使用刚体的移动方法，否则可能会导致刚体穿透
        {
            //#更改旋转值
            Quaternion worldRotation = targetRotation.ToWorld(tfHandle);//将传入的局部旋转值改为全局坐标
            rigidbody.MoveRotation(worldRotation);
        }
        else//忽略刚体：直接移动，确保能移动到正确位置
        {
            tfHandle.SetProperty(rotation: targetRotation, isLocalSpace: true);//该方法能自动使用合适的方法进行移动
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

    #region Editor
    protected override void OnValidate()
    {
        base.OnValidate();

        //【运行/非运行模式】修改Inspector中的值：同步位置
        SetModel(Config.Value);
        NotifyEvent(Config.Value);
    }

    #endregion

    #region Define
    [Serializable]
    public class ConfigInfo : ProgressInteractableConfigInfo<Vector3>
    {
        public override Vector3 Value { get { return value; } set { this.value = value; } }

        [Tooltip("The value of the rotater")]
        public Vector3 value = Vector3.zero;//局部的旋转值
    }

    public class PropertyBag : ConfigurableComponentPropertyBagBase<AD_XRRotater, ConfigInfo>
    {
    }
    #endregion
}
