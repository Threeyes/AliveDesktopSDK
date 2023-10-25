using NaughtyAttributes;
using System;
using Threeyes.Steamworks;
using UnityEngine;
/// <summary>
/// Control Environment Setting
/// PS:
/// 1.Default Environment Lighting/Reflections Sources come from Skybox, inheric this class if your want to change them
/// 
/// Warning：
/// -If you use PersistentData_SO to manage soConfig, it is necessary to set PersistentData_SO.saveAnyway to true. Otherwise, modifying the changes to soConfig by dragging sunEntity may not be saved!（如果使用PersistentData_SO来管理Config，则需要将PersistentData_SO.saveAnyway设置为true，否则通过拖拽sunEntity从而修改Config的变更可能不会被保存！）
/// 
/// 
///提供一个可选的太阳实体：
///     -可以设置太阳的颜色、亮度等（通过Gradient设置不同时段的颜色，其中X轴代表归一化的时间，HDRColor的Emission代表亮度）（Warning：不能直接使用sunSourceLight的参数，因为会影响所有物体的表面光。可以额外增加选项：是否影响灯光颜色及亮度）
///     -【重要】简单的太阳轨迹循环动画（或者根据现实太阳的轨迹进行运行）
/// PS：
/// -低于水平面时，可以不更改其颜色，因为太阳的光是恒定的，Modder可以通过模型、遮罩等进行遮挡
/// -仅提供通用属性设置，如果用户需要更改太阳物体的其他特殊属性（如材质或形状），可以另外通过其他Controller进行操作，避免该类过于复杂
/// </summary>
[ExecuteInEditMode]
[AddComponentMenu(AD_EditorDefinition.ComponentMenuPrefix_Root_Mod_Controller + "AD_DefaultEnvironmentController")]
public class AD_DefaultEnvironmentController : DefaultEnvironmentController<AD_SODefaultEnvironmentControllerConfig, AD_DefaultEnvironmentController.ConfigInfo>, IAD_EnvironmentController
{
    #region Property & Field
    [Header("Sun Entity")]//太阳实体，主要用于太阳编辑
    public AD_SunEntity sunEntity;//[Optional] Interactable gameobject to replace the sun in skybox
    #endregion

    [Header("Debug")]
    public bool isDebugMode = false;
    [EnableIf(nameof(isDebugMode))] [Range(0, 24)] public float testRealTime_Hour;//Realtime for sunEntity

    //Runtime
    Transform cacheTfSunEntity;
    Transform cacheTfMainCamera;
    Transform cacheTfSunLight;

    private void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;
#endif

        cacheTfMainCamera = ManagerHolder.EnvironmentManager.MainCamera.transform;
        cacheTfSunEntity = sunEntity?.transform;
        cacheTfSunLight = sunSourceLight?.transform;
    }
    float curPassedHourPercent { get { return curPassedHour / 24; } }//当前已用进度（百分比）
    float curPassedHour;
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            //编辑SunEntity时，同步Light及Config相关设置
            EditorUpdateConfigBasedOnSunEntity();
            return;
        }
#endif

        if (sunEntity)
        {
            ///PS：
            ///-实时计算distance而不是使用固定值，方便用户把太阳拉近拉远
            if (Config.isSunSyncWithRealTime)//自动同步时间：太阳由程序控制
            {
                DateTime dateTime = DateTime.Now;
                curPassedHour = dateTime.Hour + (float)dateTime.Minute / 60 + (float)dateTime.Second / 3600 + (float)dateTime.Millisecond / 3600000;//当天已经度过的小时（带小数，考虑Millisecond可以更方便后续缩放时更准确）（ToUpdate：考虑使用Ticks代替）
                //Debug.LogError($"Log：{curPassedHour}_{Mathf.Repeat(curPassedHour * Config.realTimeScale, 24)}");
                curPassedHour = Mathf.Repeat(curPassedHour * Config.realTimeScale, 24);//针对现实时间进行缩放
#if UNITY_EDITOR
                if (isDebugMode)
                    curPassedHour = testRealTime_Hour;
#endif
                //#1 根据时间计算位置，旋转主灯光及太阳，但不更改Config的sunLightRotation（因为是实时更新，所以不需要保存到Config中）
                float curPassedHourPercent_Shift = Mathf.Repeat(curPassedHour - 6, 24) / 24;//当前已用进度（后挪6小时），确保6、18时穿过地平线（因为当角度为0°、180°时经过地平线，所以要对时间进行位移计算）
                float angle = curPassedHourPercent_Shift * 360;//根据当天的进度计算出角度
                Quaternion targetRoatation = Quaternion.AngleAxis(angle, Config.sunRotateAxis);//计算出对应的旋转值
                cacheTfSunLight.rotation = targetRoatation;
                float distance = Vector3.Distance(cacheTfSunEntity.position, cacheTfMainCamera.position);
                cacheTfSunEntity.position = cacheTfMainCamera.position - cacheTfSunLight.forward * distance;//PS:朝向与主灯光的方向相反

                //#2 根据时间，计算太阳的颜色和亮度 (Todo：仅当curPassedHourPercent有变化时才进行修改，避免频繁调用)
                SetSunColorPercent(curPassedHourPercent);
            }
            else//非自动同步时间：用手动更改太阳位置
            {
                if (sunEntity.IsEditing)//【编辑中】：基于sunEntity的位置更新主灯光的旋转值
                {
                    Quaternion quaternionToOrigin = Quaternion.LookRotation(cacheTfMainCamera.position - cacheTfSunEntity.position);//计算太阳朝向相机的方向，用于后续正确设置直射光

                    cacheTfSunLight.rotation = quaternionToOrigin;//更新灯光旋转
                    Config.sunLightRotation = cacheTfSunLight.eulerAngles;//更新值 
                }
                else//【普通模式】：基于主灯光的旋转值更新太阳的位置，随着主相机的移动而同步变换，保证在不同位置观察，其都与主灯光/天空盒的太阳位置相同（PS：因为用户通过UIField编辑Config时base.UpdateSetting会设置灯光的旋转值，所以只需要基于灯光的旋转值进行同步即可，不需要使用Config的值）
                {
                    float distance = Vector3.Distance(cacheTfSunEntity.position, cacheTfMainCamera.position);
                    cacheTfSunEntity.position = cacheTfMainCamera.position - cacheTfSunLight.forward * distance;//PS:朝向与主灯光的方向相反

                    //cacheTfSunEntity.LookAt(cacheTfMainCamera.position);//朝向相机，方便RuntimeEditor的局部轴变换（Warning：非必要，因为朝向后Z轴不好选中）
                }

                //根据灯光的角度计算大概时间，从而计算太阳的颜色和亮度
                //Quaternion initRotation = Quaternion.AngleAxis(0, Config.sunRotateAxis);
                //Vector3 startVector= initRotation.eulerAngles;//基于sunRotateAxis计算出初始角度（ToFix：错误返回长度为0的矢量）
                Vector3 startVector = Vector3.up;//以正上方为起始角度
                float curAngle = VectorTool.Angle360(startVector, cacheTfSunLight.forward, Config.sunRotateAxis);//计算当前已经走过的角度
                float curPassedAnglePercent = curAngle / 360;
                SetSunColorPercent(curPassedAnglePercent);
            }
        }
    }

    void SetSunColorPercent(float percent)
    {
        Color hdrColor = Config.sunColorOverTime.Evaluate(percent);//基于原时间轴，对Gradient取样（PS:因为sunColorOverTime标记为HDR，因此获得的是HDR颜色， 可以直接赋值给Emission）
        Color32 basicColor;
        float intensity;
        ColorTool.DecomposeHdrColor(hdrColor, out basicColor, out intensity);//分离出基础颜色和亮度

        //对主Light进行修改（因为是实时更新，所以不需要保存到Config中）
        if (sunSourceLight)
        {
            if (Config.isSunAffectLightColor)
            {
                sunSourceLight.color = basicColor;//直接替换颜色
            }
            if (Config.isSunAffectLightIntensity)
            {
                sunSourceLight.intensity = Config.sunLightIntensity * (intensity / 10);//以sunLightIntensity作为最大值，进行缩放
            }
        }

        sunEntity.SetColorAndIntensity(hdrColor, basicColor, intensity);
    }

    #region Override
    protected override void Awake()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)//避免非运行时进入
            return;
#endif

        base.Awake();
    }
    protected override void OnDestroy()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)//避免非运行时进入
            return;
#endif

        base.OnDestroy();
    }
    protected override void UpdateSetting()
    {
        base.UpdateSetting();

        ////PS：如果用户忘了设置过isSyncWithRealTime，而用以下代码禁用XR交互，他发现无法拖拽就有可能以为是bug。所以先注释以下代码，用户移动太阳后发现太阳又回到原位，则可能会猜到问题所在。或者可以给Modder提供临时警告提示接口，当其试图移动太阳时弹出警告。
        //if (sunEntity)
        //{
        //    sunEntity.SetInteractable(!Config.isSyncWithRealTime);//如果同步，则禁用XR抓取
        //}
    }
    #endregion

    #region Define
    [Serializable]
    public class ConfigInfo : DefaultEnvironmentControllerConfigInfo
    {
        [Header("Sun Entity")]
        [Tooltip("X axis means Hour between [0, 24], and alpha means Intensity between [-10, 10]")]
        [AllowNesting] [GradientUsage(hdr: true)] public Gradient sunColorOverTime;//X轴代表时间，alpha代表亮度。（PS：如果sunSyncWithRealTime为true则根据现实时间计算，否则根据太阳与水平面的夹角计算时间）（通过将夜间的亮度调低，可以模拟出月亮的效果）（建议首尾颜色一致，并形成对称结构，避免越过中界线导致闪烁的情况）
        public bool isSunAffectLightColor = true;//使用Gradient当前值替换灯光颜色
        public bool isSunAffectLightIntensity = true;//使用Gradient当前值缩放灯光亮度

        [AllowNesting] public Vector3 sunRotateAxis = new Vector3(1, 0, 0);//太阳旋转轴。(PS：不要将轴限制在任意平面，这样便于后期太空场景的实现）
        public bool isSunSyncWithRealTime = false;//与现实时间同步(24小时值)，默认6、18时经过地平面。设置为true会由程序控制太阳轨迹，设置为false则可以自由拖动太阳
        [EnableIf(nameof(isSunSyncWithRealTime))] [Range(1, 10000)] public float realTimeScale = 1f;//针对真实时间的缩放

        //ToDelete：以下字段不好判断是否更改，可以改为后期使用Properbag直接保存SunEntity的信息
        //public float sunDistance = 100;//（保存默认的距离，以及用户修改后的距离）（暂不做范围限制）（Bug：编辑模式时容易导致变动，因为此时VR相机到处移动）
        //public Vector3 sunSize = Vector3.one;//太阳尺寸

        //ToAdd：Fog
    }
    #endregion

    #region Utility

    #endregion

    #region Editor Method
#if UNITY_EDITOR

    Quaternion editorLastLightQuaternion;
    /// <summary>
    /// 在非运行模式编辑SunEntity时，会同步更新相关设置
    /// </summary>
    //[ContextMenu("SetConfigLightAngleBasedOnSunEntityPosition")]
    void EditorUpdateConfigBasedOnSunEntity()
    {
        //因为可能程序未运行，因此目标点为世界坐标原点
        if (sunEntity && sunSourceLight)
        {
            Quaternion quaternionToOrigin = Quaternion.LookRotation(Vector3.zero - sunEntity.transform.position);//计算太阳朝向相机的方向，用于后续正确设置直射光

            if (quaternionToOrigin != editorLastLightQuaternion)
            {
                sunSourceLight.transform.rotation = quaternionToOrigin;//更新灯光旋转
                Config.sunLightRotation = sunSourceLight.transform.eulerAngles;//更新值 
                editorLastLightQuaternion = quaternionToOrigin;
                if (soOverrideConfig)
                    UnityEditor.EditorUtility.SetDirty(soOverrideConfig);//PS:需要调用该方法保存更改
            }
        }
    }


    //——MenuItem——
    static string instName = "DefaultEnvironmentController";
    [UnityEditor.MenuItem(AD_EditorDefinition.HierarchyMenuPrefix_Root_Mod_Controller_Environment + "Default", false)]
    public static void CreateInst()
    {
        Threeyes.Editor.EditorTool.CreateGameObjectAsChild<AD_DefaultEnvironmentController>(instName);
    }
#endif
    #endregion
}