using System;
using Threeyes.Core;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
///  (Move along local +Z Axis), an interactable that follows the position of the interactor on a single axis
/// 滑动条（沿着+Z轴）
/// 
/// PS:
/// -控制指定物体在局部Z轴的位置，可用于抽屉、音量条等
/// 
/// ToAdd:
/// -后期支持可选通过ConfigableJoint实现，并且在受外界影响而更改Slider后会同步更新到Config的相应字段中
/// 
/// 功能：
/// -支持运行时缩放根物体
/// -支持加载后还原到上次的位置
/// 
/// Warning：
/// -Handle需要有父物体，否则会计算错误！（暂不支持无父物体，负责需要进行额外计算）
/// 
/// Ref:UnityEngine.XR.Content.Interaction.XRSlider
/// </summary>
public class AD_XRSlider : AD_XRProgressInteractable<AD_XRSlider, AD_XRSlider.ConfigInfo, AD_XRSlider.PropertyBag, float>
{
    #region Property & Field
    [SerializeField]
    [Tooltip("Events to trigger when the slider is moved")]
    FloatEvent onValueChanged = new FloatEvent();
    #endregion

    #region Unity Method
    protected override void LateUpdate_NoGrabbingState()
    {
        //#1 （当物体受外力影响后）计算Handle当前朝向对应的Value
        float curPercent = 0;
        if (Config.minPosition < Config.maxPosition)
        {
            curPercent = (tfHandle.localPosition.z - Config.minPosition) / (Config.maxPosition - Config.minPosition);
        }

        //#2 更新Config中的数据
        UpdateValueIfChanged(curPercent);
    }
    #endregion

    #region Interaction
    /// <summary>
    /// 更新交互
    /// </summary>
    protected override void ProcessInteractableFunc()
    {
        //#1 根据抓取信息计算Value
        //-根据抓取的相对位移值在(范围区间的占比)来计算sliderValue
        // Put anchor position into slider space
        Vector3 curGrabLocalPosition = GetGrabLocalPosition();
        Vector3 localGrabOffset = curGrabLocalPosition - lastGrabLocalPos;//计算与上次抓取的位移值
        lastGrabLocalPos = curGrabLocalPosition;
        //Debug.LogError("[ToDelete]: " + curGrabLocalPosition + " " + localOffset);

        float offsetToDeltaValue = localGrabOffset.z / (Config.maxPosition - Config.minPosition);//将位移转为归一的位移值
        var sliderValue = Mathf.Clamp01(Config.value + offsetToDeltaValue);

        //#2 更新Config中的数据
        Config.Value = sliderValue;

        //#3 通知模型及事件更新
        SetModel(sliderValue);
        NotifyEvent(sliderValue);
    }

    protected override void SetModel(float modelValue, bool ignoreRigidbody = false)
    {
        if (tfHandle == null)
            return;

        Vector3 handlePos = tfHandle.localPosition;

        //重置X、Y轴，避免因为Rigidbody导致其他轴出现异常
        handlePos.x = 0;
        handlePos.y = 0;
        handlePos.z = Mathf.Lerp(Config.minPosition, Config.maxPosition, modelValue);

        Rigidbody rigidbody = tfHandle.GetComponent<Rigidbody>();
        if (Application.isPlaying && !ignoreRigidbody && rigidbody)//【运行模式】如果目标是刚体， 则使用刚体的移动方法，否则可能会导致刚体穿透
        {
            Vector3 worldPos = tfHandleParent.TransformPoint(handlePos);//基于父物体坐标系，将局部坐标转为全局坐标给Rigidbody使用
            rigidbody.MovePosition(worldPos);
            //Debug.LogError("[ToDelete]: " + handlePos + " " + worldPos);
        }
        else//忽略刚体：直接移动，确保能移动到正确位置
        {
            tfHandle.SetProperty(handlePos);//该方法能自动使用合适的方法进行移动
            //m_Handle.localPosition = handlePos;
        }
    }
    /// <summary>
    /// 设置Handle到值对应的状态，并调用Event
    /// </summary>
    /// <param name="value"></param>
    /// <param name="ignoreRigidbody">是否忽略刚体，在初始化时需要忽略，否则可能会无法还原到正常位置</param>
    protected override void NotifyEvent(float value)
    {
        onValueChanged.Invoke(value);
    }
    #endregion

    #region Editor
    protected override void OnDrawGizmosSelected()
    {
        var sliderMinPoint = transform.TransformPoint(new Vector3(0.0f, 0.0f, Config.minPosition));
        var sliderMaxPoint = transform.TransformPoint(new Vector3(0.0f, 0.0f, Config.maxPosition));

        Gizmos.color = Color.green;
        Gizmos.DrawLine(sliderMinPoint, sliderMaxPoint);
    }
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
    public class ConfigInfo : ProgressInteractableConfigInfo<float>
    {
        public override float Value { get { return value; } set { this.value = value; } }

        [Tooltip("The value of the slider")] [Range(0.0f, 1.0f)] public float value = 0.5f;

        [Tooltip("The offset of the slider at value '0'")] public float minPosition = 0f;
        [Tooltip("The offset of the slider at value '1'")] public float maxPosition = 1f;
    }

    public class PropertyBag : ConfigurableComponentPropertyBagBase<AD_XRSlider, ConfigInfo>
    {
    }
    #endregion
}