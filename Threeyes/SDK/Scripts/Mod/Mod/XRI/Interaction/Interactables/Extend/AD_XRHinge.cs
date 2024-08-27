using System;
using Threeyes.Core;
using Threeyes.GameFramework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// （Rotate Around local X Axis ）An interactable lever that snaps into an on or off position by a direct interactor, support HingeJoint.
/// 铰链（绕局部X轴顺时针旋转，局部forward为默认旋转原点）,支持与HingeJoint组件联合使用
/// 
/// 特点：
/// -可以和HingeJoint联合使用，抓取时由该组件管理Value，其他情况由HingeJoint管理Value（此时可以与物理引擎交互）
/// -会跟随抓取点的朝向，如果使用Direct Interactor会很跟手
/// 
/// 
/// Ref：XRJoystick
/// </summary>
public class AD_XRHinge : AD_XRProgressInteractable<AD_XRHinge, AD_XRHinge.ConfigInfo, AD_XRHinge.PropertyBag, float>
{
    #region Property & Field
    [SerializeField]
    [Tooltip("Events to trigger when the hinge is moved")]
    FloatEvent onValueChanged = new FloatEvent();
    #endregion

    #region Unity Method
    protected override void LateUpdate_NoGrabbingState()
    {
        ///#非抓取模式下：由外力（如HingeJoint）更新Value
        ///PS:
        ///-此时由HingeJoint控制物体的旋转角度，注意此时如果HingeJoint的角度限制与Config不一致可能会出现越界，但不影响反序列化，暂时不做限定以免与Rigidbody冲突。
        ///
        ///ToUpdate:
        ///-后续任何继承AD_XRProgressInteractable的组件都可以这样进行更新，不用限定

        //#1 （当物体受HingJoint等外力影响后）计算Handle当前朝向对应的Value
        float curPercent = ConvertInputToValue(tfHandle.forward);

        //#2 更新Config中的数据
        UpdateValueIfChanged(curPercent);
    }
    #endregion

    #region Interaction
    protected override void StartGrabBeginFunc(SelectEnterEventArgs args)
    {
        base.StartGrabBeginFunc(args);

        ////计算抓取点与Handle的偏差
        //Vector3 lastHandleForward = tfHandle.forward;
        //Quaternion lookRotation =; //Creates a rotation with the specified forward and upwards directions.(创建一个从当前旋转值到目标forward的旋转值)

        grabBeginDeltaAngle = ConvertPosToAngle(lastGrabLocalPos) - ConvertPosToAngle(tfHandleParent.InverseTransformVector(tfHandle.forward));//计算当前抓取点与Handle矢量的偏差角度
    }
    public float grabBeginDeltaAngle = 0;//GrabBegin时，抓取点   朝向与Handle朝向的夹角
    protected override void ProcessInteractableFunc()
    {
        //////——实现方式1：根据当前抓取点计算对应角度。（【Bug】：当开始抓住非正前方模型时会顺序）——
        ////#1 计算局部抓取点对应的旋转值
        //float grabPercent = ConvertPosToValue(GetGrabLocalPosition());

        ////#2 更新Config中的数据
        //Config.Value = grabPercent;
        //SetModel(grabPercent);//或者由上方直接调用SetModelFunc(Quaternion rotation），注释掉这个多余的方法，只调用下方的事件通知
        //NotifyEvent(grabPercent);


        //——实现方式2：在1的基础上，加上初始偏移角度——
        //#0 获取抓取点
        float curGrabAngle = ConvertPosToAngle(GetGrabLocalPosition(), true);
        float handleAngle = Mathf.Repeat(curGrabAngle - grabBeginDeltaAngle, 360);//去掉初始位移角度，从grab反推出handle的角度
        float targetPercent = ConvertAngleToValue(handleAngle);//Mathf.Clamp01(targetPercentRaw);
        //Debug.LogError($"Info:  {curGrabAngle} -  {grabBeginDeltaAngle} =  {handleAngle} ({targetPercent})");

        //#2 更新Config中的数据
        Config.Value = targetPercent;
        SetModel(targetPercent);//或者由上方直接调用SetModelFunc(Quaternion rotation），注释掉这个多余的方法，只调用下方的事件通知
        NotifyEvent(targetPercent);


        ////——实现方式3：计算位移（【Bug】：不跟收）——
        /////ToUpdate:
        /////-应该是根据抓取位置，计算出目标的朝向角度，而不是直接使用抓取点来计算矢量
        //float grabPercentOffset = 0;
        /////ToUpdate：应该是计算与
        ////#1 计算新旧抓取点的值差
        //Vector3 curGrabLocalPosition = GetGrabLocalPosition();
        //float lastGrabPercent = ConvertPosToValue(lastGrabLocalPos);
        //float curGrabPercent = ConvertPosToValue(curGrabLocalPosition/*, true*/);
        ////grabPercentOffset = curGrabPercent - lastGrabPercent;

        //float grabAngleOffset = ConvertPosToAngle(curGrabLocalPosition, true) - ConvertPosToAngle(lastGrabLocalPos);
        //if (grabAngleOffset < Config.maxAngle - Config.minAngle)//避免反转
        //{
        //    grabPercentOffset = grabAngleOffset / (Config.maxAngle - Config.minAngle);
        //}

        //lastGrabLocalPos = curGrabLocalPosition;//仅在值有效时保存

        ////Debug.LogError("Shit: " + lastGrabPercent + " || " + curGrabPercent + " || " + grabPercentOffset);
        //Debug.LogError("Shit: " + lastGrabPercent + " || " + curGrabPercent + " || " + grabAngleOffset);

        //float grabPercent = lastGrabPercent;
        ////if (Mathf.Abs(grabPercentOffset) < 1)//确保是有效值：避免因为反向角度导致瞬移(应该是确保角度不超过最大-最小)
        //{
        //}

        //grabPercent = Mathf.Clamp01(Config.value + grabPercentOffset);
        ////lastGrabLocalPos = curGrabLocalPosition;//仅在值有效时保存
        ////#2 更新Config中的数据
        //Config.Value = grabPercent;
        //SetModel(grabPercent);//或者由上方直接调用SetModelFunc(Quaternion rotation），注释掉这个多余的方法，只调用下方的事件通知
        //NotifyEvent(grabPercent);
    }

    float ConvertPosToValue(Vector3 localPos, bool debugLogAngle = false)
    {
        //#0 计算点在YZ平面的 朝向矢量
        Vector3 curGrabLocalPosition = localPos;
        curGrabLocalPosition.x = 0;

        //#1 计算矢量对应的旋转值
        return ConvertInputToValue(tfHandleParent.TransformDirection(curGrabLocalPosition), debugLogAngle);
    }

    float ConvertPosToAngle(Vector3 localPos, bool debugLogAngle = false)
    {
        //#0 计算点在YZ平面的 朝向矢量
        Vector3 curGrabLocalPosition = localPos;
        curGrabLocalPosition.x = 0;

        //#1 计算矢量对应的旋转值
        return ConvertInputToAngle(tfHandleParent.TransformDirection(curGrabLocalPosition), debugLogAngle);
    }

    /// <summary>
    /// 计算矢量最近的Value
    /// </summary>
    /// <param name="worldForward">朝向</param>
    /// <returns></returns>
    float ConvertInputToValue(Vector3 worldForward, bool debugLogAngle = false)
    {
        float angle = ConvertInputToAngle(worldForward, debugLogAngle);//计算矢量朝向与HandleParent Z轴 的夹角，从而计算角度
        return ConvertAngleToValue(angle);
    }

    float ConvertAngleToValue(float angle)
    {
        //对角度进行裁剪
        float percent = 0;
        if (Config.minAngle < Config.maxAngle)//检测配置是否正确，避免用户设置错误导致报错
        {
            if (angle > Config.minAngle && angle < Config.maxAngle)//抓取夹角在范围内：计算百分比
            {
                percent = (angle - Config.minAngle) / (Config.maxAngle - Config.minAngle);
            }
            else//抓取夹角在范围外：找到距离最近的一个端点；或者根据其上一帧的偏转值来判断
            {
                //Debug.LogError($"Test: {grabAngle.DeltaAngle(Config.minAngle)}_ {grabAngle.DeltaAngle(Config.maxAngle)}");

                percent = Mathf.Abs(angle.DeltaAngle(Config.minAngle)) < Mathf.Abs(angle.DeltaAngle(Config.maxAngle)) ? 0 : 1;//计算grabAngle与最大/最小角度的夹角，间隔值越小代表越近
            }
        }
        return percent;
    }

    float ConvertInputToAngle(Vector3 worldForward, bool debugLogAngle = false)
    {
        float angle = tfHandleParent.forward.DeltaAngle360(worldForward, tfHandleParent.right);//计算从HandleParent.forward到目标矢量的夹角，从而计算角度
        //if (debugLogAngle)
        //    Debug.LogError("DebugAngle: " + angle);
        return angle;
    }

    protected override void SetModel(float modelValue, bool ignoreRigidbody = false)
    {
        if (tfHandle == null)
            return;

        float targetAngle = Mathf.Lerp(Config.minAngle, Config.maxAngle, modelValue);//目标角度
        Quaternion initRotation = tfHandleParent.GetSelfCoordinateSystemInitRotation();//计算基于父物体坐标的初始旋转值（PS：之所以不直接使用父物体的旋转值，是因为父物体可能有初始偏转，从而导致后续计算错误）
        Quaternion targetRotation = initRotation.RotateAround(Vector3.right, targetAngle);//绕该初始旋转值绕其Z轴旋转目标角度

        SetModelFunc(targetRotation, ignoreRigidbody);
    }

    protected void SetModelFunc(Quaternion rotation, bool ignoreRigidbody = false)
    {
        //设置Handle（forward）的角度
        Rigidbody rigidbody = tfHandle.GetComponent<Rigidbody>();
        if (Application.isPlaying && !ignoreRigidbody && rigidbody)//【运行模式】如果目标是刚体， 则使用刚体的移动方法，否则可能会导致刚体穿透
        {
            rigidbody.MoveRotation(rotation);
        }
        else//忽略刚体：直接移动，确保能移动到正确位置
        {
            tfHandle.SetProperty(rotation: rotation, isLocalSpace: false);//该方法能自动使用合适的方法进行移动
        }
    }

    protected override void NotifyEvent(float value)
    {
        onValueChanged.Invoke(value);
    }
    #endregion

    #region Editor
    protected override void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (Config.minAngle >= Config.maxAngle)//避免范围值翻转
            {
                float minOffsetValue = 1;
                Vector2 range = new Vector2(0, 360);
                if (Config.minAngle == range.x || Config.maxAngle < range.x + minOffsetValue)
                {
                    Config.maxAngle = Config.minAngle + minOffsetValue;
                }
                else
                {
                    Config.minAngle = Config.maxAngle - minOffsetValue;
                }
            }
        }

        base.OnValidate();
        //【运行/非运行模式】修改Inspector中的值：同步位置
        SetModel(Config.Value);
        NotifyEvent(Config.Value);
    }
    #endregion

    #region Define
    [Serializable]
    public class ConfigInfo : ProgressInteractableConfigInfo<float>
    {
        public override float Value { get { return value; } set { this.value = value; } }

        [Tooltip("The value of the hinge (Percent)")] [Range(0.0f, 1.0f)] public float value = 0.5f;

        //（如果minAngle为0且maxAngle为360，则物体能够不受限制地绕轴旋转）
        [Tooltip("The rotation of the hinge at value '0'")] [Range(0f, 360f)] public float minAngle = 0f;
        [Tooltip("The rotation of the hinge at value '1'")] [Range(0f, 360f)] public float maxAngle = 360f;
    }

    public class PropertyBag : ConfigurableComponentPropertyBagBase<AD_XRHinge, ConfigInfo>
    {
    }
    #endregion
}