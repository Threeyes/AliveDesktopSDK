using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Threeyes.Core;
using Threeyes.RuntimeEditor;
using Threeyes.Steamworks;
using UnityEngine;
using System.Linq;
/// <summary>
/// Control Environment Setting
/// PS:
/// 1.Default Environment Lighting/Reflections Sources come from Skybox, inheric this class if your want to change them
///
/// Todo:
/// -【重要】SunEntity、Skybox都可以作为一个单独的预制物，方便直接替换。注意场景可能同时有多个被用户加入的物体。解决办法：
///     -#1 只有最后一个的有效，其余的在Inspector中提示警告（调用TrySetSkybox方法， 并且依次入List，销毁时出栈。这样可以保证场景剩余的自定义Skybox有效）（可以用REButton来动态绘制提示）
///     -#2 在尝试加入第二个时弹出警告（可以通过生成前，调用对应的验证组件，检查根物体是否包含有RuntimeEditorDisallowMultipleObject的标识组件）
/// -用户添加的ReflectionProbeController可以有多个实例，并可以设置是否跟随环境回调自动更新（默认为false，避免频繁调用导致卡顿）
/// 
/// Warning：
/// -If you use PersistentData_SO to manage soConfig, it is necessary to set PersistentData_SO.saveAnyway to true. Otherwise, modifying the changes to soConfig by dragging sunEntity may not be saved!（如果使用PersistentData_SO来管理Config，则需要将PersistentData_SO.saveAnyway设置为true，否则通过拖拽sunEntity从而修改Config的变更可能不会被保存！）
/// 
/// 
///提供一个可选的太阳实体：
///     -可以设置太阳的颜色、亮度等（通过Gradient设置不同时段的颜色，其中X轴代表归一化的时间，HDRColor的Emission代表亮度）（Warning：不能直接使用sunSourceLight的参数，因为会影响所有物体的表面光。可以额外增加选项：是否影响灯光颜色及亮度）
///     -【重要】简单的太阳轨迹循环动画（或者根据现实太阳的轨迹进行运行）
/// 
/// PS：
/// -AD_SunEntity作为一个特殊的脚本，因为与全局光绑定，所以场景只能同时存在一个。后续可以增加不影响全局光的通用天体（如月亮、星星）
/// -低于水平面时，可以不更改其颜色，因为太阳的光是恒定的，Modder可以通过模型、遮罩等进行遮挡
/// -仅提供通用属性设置，如果用户需要更改太阳物体的其他特殊属性（如材质或形状），可以另外通过其他Controller进行操作，避免该类过于复杂
/// </summary>
[AddComponentMenu(AD_EditorDefinition.ComponentMenuPrefix_Root_Mod_Controller + "AD_DefaultEnvironmentController")]
public class AD_DefaultEnvironmentController : DefaultEnvironmentController<AD_SODefaultEnvironmentControllerConfig, AD_DefaultEnvironmentController.ConfigInfo>, IAD_EnvironmentController
{
    [Header("Debug")]
    public bool isDebugMode = false;
    [EnableIf(nameof(isDebugMode))] [Range(0, 24)] public float testRealTime_Hour;//Realtime for sunEntity

    //#Runtime
    Transform tfActiveSunEntity { get { return ActiveSunEntityController.transform; } }
    Transform tfMainCamera { get { return AD_ManagerHolder.XRManager.TfCameraEye; } }
    Transform tfSunLight { get { return sunSourceLight.transform; } }

    #region SunEntity (Todo:移动到父类或接口)
    AD_SunEntityController ActiveSunEntityController
    {
        get
        {
            if (sunEntityController_Custom)//优先使用自定义的SunEntity
                return sunEntityController_Custom;
            return null;
        }
    }
    AD_SunEntityController sunEntityController_Custom;//运行时自定义的SunEntity（为了避免被意外销毁导致为空，需要单独使用该字段保存Custom的数据）
    List<AD_SunEntityController> listCustomSunEntityController = new List<AD_SunEntityController>();
    public void RegisterCustomSunEntity(AD_SunEntityController customSunEntityController)
    {
        listCustomSunEntityController.AddOnce(customSunEntityController);
        listCustomSunEntityController.Remove(null);//移除可能因场景切换等情况被删除的实例
        if (!Config.isUseSkybox)
            return;

        TrySetSunEntity();
    }
    public void UnRegisterCustomSunEntity(AD_SunEntityController customSunEntityController)
    {
        if (listCustomSunEntityController.Count > 0)
        {
            listCustomSunEntityController.Remove(customSunEntityController);
            listCustomSunEntityController.Remove(null);//移除可能被删除的实例
        }
        TrySetSunEntity();
    }

    void TrySetSunEntity()
    {
        //找到目标
        sunEntityController_Custom = listCustomSunEntityController.LastOrDefault();//获取最后加入的SunEntiry（不管是否为null）

        //初始化该Custom物体的缩放、位置、材质等(先暂时以【普通模式】【不同步时间】状态进行初始化，如果当前设置为【同步时间】，则由Update处理后续更新)
        AD_SunEntityController activeSE = ActiveSunEntityController;//激活的
        if (activeSE)
        {
            listCustomSunEntityController.ForEach(sE => sE.SetActive(sE == activeSE));//只激活当前有效的SunEntity，避免场景存在多个

            activeSE.transform.localScale = Config.sunEntitySize;
            activeSE.transform.position = GetSunEntityPosition_NoSyncTime();
            SetSunColorOverTime_NoSyncTime();//设置颜色
        }

        TrySetActiveProceduralSkybox_SunSize();
    }

    protected override bool TrySetActiveProceduralSkybox_SunSizeFunc(Material activeSkyboxMaterial)
    {
        if (ActiveSunEntityController)//只要使用了SunEntity，那么就把SunSize设置为0。避免天空出现多个太阳。
        {
            if (activeSkyboxMaterial.HasFloat(ShaderID_SunSize))
                if (activeSkyboxMaterial.GetFloat(ShaderID_SunSize) != 0)
                {
                    activeSkyboxMaterial.SetFloat(ShaderID_SunSize, 0);
                    return true;
                }
            return false;
        }
        else
            return base.TrySetActiveProceduralSkybox_SunSizeFunc(activeSkyboxMaterial);
    }



    float curPassedHourPercent { get { return curPassedHour / 24; } }//当前已用进度（百分比）
    float curPassedHour;
    Vector3 cacheLastSunEntitySize;
    float cacheLastSunEntityDistance;
    private void Update()
    {
        if (hasDeinit)//避免卸载时仍修改Config
            return;

        //#if UNITY_EDITOR
        //        if (!Application.isPlaying) //非运行模式编辑SunEntity时，同步Light及Config相关设置
        //        {
        //            //EditorUpdateConfigBasedOnSunEntity();
        //            return;
        //        }
        //#endif

        if (ActiveSunEntityController)
        {
            ///#1 更新SunEntity的各种数值
            float distance = Config.sunEntityDistance;//基于Config而不是实时距离，适用于编辑中及普通模式

            /// PS：
            ///-实时计算distance而不是使用固定值，方便用户把太阳拉近拉远
            if (Config.isSunEntitySyncWithRealTime)//自动同步时间：由程序控制太阳的位置。PS：此时
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
                Quaternion targetRoatation = Quaternion.AngleAxis(angle, Config.sunEntityRotateAxis);//计算出对应的旋转值
                tfSunLight.rotation = targetRoatation;
                tfActiveSunEntity.position = tfMainCamera.position - tfSunLight.forward * distance;//PS:朝向与主灯光的方向相反

                //#2 根据时间，计算太阳的颜色和亮度 (Todo：仅当curPassedHourPercent有变化时才进行修改，避免频繁调用)
                SetSunColorPercent(curPassedHourPercent);
            }
            else//非自动同步时间：手动更改太阳位置
            {
                if (ActiveSunEntityController.IsEditing)//【编辑中】：此时由用户控制sunEntity的位置，基于位置自动更新并保存主灯光的旋转值
                {
                    //同步主灯光的旋转值（因为值会一直变动，所以不需要像说缩放一样进行对比）
                    Quaternion quaternionToOrigin = Quaternion.LookRotation(tfMainCamera.position - tfActiveSunEntity.position);//计算太阳朝向相机的方向，用于后续正确设置直射光
                    tfSunLight.rotation = quaternionToOrigin;//更新灯光旋转
                    Config.sunLightRotation = tfSunLight.eulerAngles;//保存值（PS：isSunSyncWithRealTime为true时不需要保存）
                }
                else//【普通模式】：基于主灯光的旋转值更新反推太阳的位置，随着主相机的移动而同步变换，保证在不同位置观察，其都与主灯光/天空盒的太阳位置相同
                {
                    tfActiveSunEntity.position = GetSunEntityPosition_NoSyncTime();//PS:朝向与主灯光的方向相反
                }

                SetSunColorOverTime_NoSyncTime();
            }
            if (Config.isSunEntityFaceCamera)//朝向相机
                tfActiveSunEntity.LookAt(tfMainCamera.position);


            //#2【编辑中】：保存其被用户更改的影响Config的属性(如Transform相关的distance、Szie)
            if (ActiveSunEntityController.IsEditing)
            {
                //更新缩放
                Vector3 curSunEntitySize = tfActiveSunEntity.localScale;
                if (cacheLastSunEntitySize != curSunEntitySize)
                {
                    Config.sunEntitySize = curSunEntitySize;
                    cacheLastSunEntitySize = curSunEntitySize;
                }

                //更新距离
                float curSunEntityDistance = Vector3.Distance(tfActiveSunEntity.position, tfMainCamera.position);
                if (cacheLastSunEntityDistance != curSunEntityDistance)
                {
                    Config.sunEntityDistance = curSunEntityDistance;
                    cacheLastSunEntityDistance = curSunEntityDistance;
                }
            }
        }

        //不管有无 SunEntity，都需要尝试更新反射（SunEntity主要用于可视化更改Config，Reflection是否更新主要又其他参数决定）
        TryAutoUpdateReflection();
    }

    //根据灯光的角度计算大概的一天进度，从而计算太阳的颜色和亮度
    void SetSunColorOverTime_NoSyncTime()
    {
        Vector3 startVector = Vector3.up;//以正上方为起始角度
        float curAngle = VectorTool.Angle360(startVector, tfSunLight.forward, Config.sunEntityRotateAxis);//基于旋转轴，计算当前已经走过的角度
        float curPassedAnglePercent = curAngle / 360;
        SetSunColorPercent(curPassedAnglePercent);
    }
    /// <summary>
    /// 获取SunEntity在【普通模式】【不同步时间】时的位置
    /// </summary>
    /// <returns></returns>
    Vector3 GetSunEntityPosition_NoSyncTime()
    {
        ///PS:
        ///- 因为用户通过UIField编辑Config时base.UpdateSetting会设置灯光的旋转值，所以只需要基于灯光的实时朝向进行计算，不能直接使用Config的值
        ///- SunEntity的朝向与主灯光的方向相反(不管ActiveSunEntityController是否为空，都能正常计算）
        return tfMainCamera.position - tfSunLight.forward * Config.sunEntityDistance;
    }


    Vector3 lastSunLightRotation_AutoUpdate = Vector3.negativeInfinity;
    /// <summary>
    /// 根据时间间隔，尝试自动更新反射探头
    /// </summary>
    void TryAutoUpdateReflection()
    {
        if (!Config.isAutoUpdateReflectionProbe)
            return;

        //#1检查真实时间间隔，避免频繁更新导致程序卡顿
        if (Time.time - lastUpdateReflectionProbeTime < Config.updateReflectionProbeIntervalTime)
            return;

        //#2 检查角度变化
        if (lastSunLightRotation_AutoUpdate.Equals(Vector3.negativeInfinity))//Init
        {
            lastSunLightRotation_AutoUpdate = Config.sunLightRotation;
            return;
        }
        else
        {
            if (Vector3.Angle(tfSunLight.eulerAngles, lastSunLightRotation_AutoUpdate) < Config.updateReflectionProbeIntervalAngle)//ToUpdate:应该时获取当前灯光旋转值
                return;
            else
                lastSunLightRotation_AutoUpdate = tfSunLight.eulerAngles;
        }
        RefreshReflectionProbe();//PS：会自动更新lastUpdateReflectionProbeTime
    }

    void SetSunColorPercent(float percent)
    {
        Color hdrColor = Config.sunEntityColorOverTime.Evaluate(percent);//基于原时间轴，对Gradient取样（PS:因为sunColorOverTime标记为HDR，因此获得的是HDR颜色， 可以直接赋值给Emission）
        Color32 basicColor;
        float intensity;
        ColorTool.DecomposeHdrColor(hdrColor, out basicColor, out intensity);//分离出基础颜色和亮度

        //对主Light进行修改（因为是实时更新，所以不需要保存到Config中）
        if (sunSourceLight)
        {
            if (Config.isSunEntityAffectLightColor)
            {
                sunSourceLight.color = basicColor;//直接替换颜色
            }
            if (Config.isSunEntityAffectLightIntensity)
            {
                sunSourceLight.intensity = Config.sunLightIntensity * Config.sunEntityLightIntensityScale * (intensity / 10);//以sunLightIntensity作为最大值，进行缩放（intensity的范围为[0,10]，因此要缩小10倍）
            }
        }

        ActiveSunEntityController.SetColorAndIntensity(basicColor, hdrColor, intensity);
    }
    #endregion

    #region Override
    bool hasDeinit = false;
    public override void OnModControllerDeinit()
    {
        base.OnModControllerDeinit();
        hasDeinit = true;
    }
    protected override void UpdateSetting()
    {
        //Warning：以下调用不能使用cache字段，否则会因为未初始化而报错
        base.UpdateSetting();

        if (ActiveSunEntityController)
        {
            ActiveSunEntityController.transform.localScale = Config.sunEntitySize;//Wanring:此时cacheTfSunEntity不一定被初始化，暂不使用其字段

            ///PS：如果用户忘了设置过isSyncWithRealTime，而用以下代码禁用XR交互，他发现无法拖拽就有可能以为是bug。所以先注释以下代码，用户移动太阳后发现太阳又回到原位，则可能会猜到问题所在。或者可以给Modder提供临时警告提示接口，当其试图移动太阳时弹出警告。
            //sunEntity.SetInteractable(!Config.isSyncWithRealTime);//如果同步，则禁用XR抓取

            if (sunSourceLight)
            {
                float distance = Config.sunEntityDistance;//基于Config而不是实时距离，适用于编辑中及普通模式
                ActiveSunEntityController.transform.position = tfMainCamera.position - sunSourceLight.transform.forward * distance;//PS:朝向与主灯光的方向相反
            }
        }
    }
    #endregion

    #region Editor Method
#if UNITY_EDITOR

    public AD_SunEntityController editorSunEntityController;//Use this to auto set Config's sunEntity related property（e.g., A preset decoration item with AD_SunEntityController component）
    ///ToUpdate：
    ///-添加 EditorUpdateConfigUsingComponentData 组件，基于
    [ContextMenu("UpdateConfigUsingComponentData")]
    void EditorUpdateConfigUsingComponentData()
    {
        EditorUpdateConfigUsingComponentDataFunc();
    }

    protected override void EditorUpdateConfigUsingComponentDataFunc()
    {
        base.EditorUpdateConfigUsingComponentDataFunc();

        //#基于editorSunEntityController设置SunEntity相关属性
        if (!editorSunEntityController)
            return;
        if (!sunSourceLight)
        {
            Debug.LogError($"{nameof(sunSourceLight)} can't be null!");
            return;
        }

        var targetConfig = soOverrideConfig ? soOverrideConfig.config : defaultConfig;//不能直接通过Config获取！因为其会导致config被初始化，从而无法检测对config的修改


        //计算太阳朝向相机的方向，用于后续正确设置直射光
        Quaternion quaternionToOrigin = Quaternion.LookRotation(Vector3.zero - editorSunEntityController.transform.position);        //因为程序可能未运行，因此暂定目标点为世界坐标原点（粗略计算，假设距离够远远）。后续可基于camera的位置，或XR的默认点位置
        sunSourceLight.transform.rotation = quaternionToOrigin;//更新灯光物体的旋转
        targetConfig.sunLightRotation = sunSourceLight.transform.eulerAngles;//更新Config的旋转值
        targetConfig.sunEntityDistance = Vector3.Distance(Vector3.zero, editorSunEntityController.transform.position);//更新Config的距离值

        targetConfig.sunEntitySize = editorSunEntityController.transform.localScale;

        //Debug.Log("Environment Config has changed");
        if (soOverrideConfig)//SO：需要持久化存储SOAsset
        {
            UnityEditor.EditorUtility.SetDirty(soOverrideConfig);//PS:需要调用该方法保存更改
        }
        else//defaultConfig：通知存储组件
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }


    //——MenuItem——
    static string instName = "DefaultEnvironmentController";
    [UnityEditor.MenuItem(AD_EditorDefinition.HierarchyMenuPrefix_Root_Mod_Controller_Environment + "Default", false)]
    public static void CreateInst()
    {
        Threeyes.Core.Editor.EditorTool.CreateGameObjectAsChild<AD_DefaultEnvironmentController>(instName);
    }
#endif
    #endregion

    #region Define
    [Serializable]
    public class ConfigInfo : DefaultEnvironmentControllerConfigInfo
    {
        [Header("Sun Entity")]
        public bool isSunEntityFaceCamera = true;
        public Vector3 sunEntityRotateAxis = new Vector3(1, 0, 0);//太阳旋转的轴（用于根据时间计算SunEntity，以及基于时间反推SunEntity位置）(PS：不要将轴限制在任意平面，这样便于后期太空场景的实现）（命名参考HingeJonint）
        public Vector3 sunEntitySize = Vector3.one * 10;//缓存太阳尺寸，方便用户随意修改
        [Min(1)] public float sunEntityDistance = 100;//（保存默认的距离，以及用户修改后的距离）（Warning:因为与相机的位置相关，如果相机无法还原到上次退出的位置（如【VR模式】），那么太阳也可能不会还原到正确的位置。后期可以额外存储退出时的位置，方便还原处于Socket的情况）（Warning：距离不能为0，否则会因为太阳一直推着走，导致类似飞行的效果。）
        [Tooltip("X axis means Hour between [0, 24], and alpha means Intensity between [-10, 10]")]
        [AllowNesting] [GradientUsage(hdr: true)] public Gradient sunEntityColorOverTime = new Gradient();//X轴代表时间，alpha代表亮度。（PS：如果sunSyncWithRealTime为true则根据现实时间计算，否则根据太阳与水平面的夹角计算时间）（通过将夜间的亮度调低，可以模拟出月亮的效果）（建议首尾颜色一致，并形成对称结构，避免越过中界线导致闪烁的情况）
        public bool isSunEntityAffectLightColor = true;//使用Gradient当前值替换灯光颜色
        public bool isSunEntityAffectLightIntensity = true;//使用Gradient亮度值影响灯光亮度
        [Min(0)] public float sunEntityLightIntensityScale = 5;//使用Gradient亮度值缩放灯光亮度(使用该字段，可以确保sunLightIntensity值可以保证在正常区间)

        public bool isSunEntitySyncWithRealTime = false;//与现实时间同步(24小时值)，默认6、18时经过地平面。设置为true会由程序控制太阳轨迹，设置为false则可以自由拖动太阳
        [EnableIf(nameof(isSunEntitySyncWithRealTime))] [Range(1, 10000)] public float realTimeScale = 1f;//针对真实时间的缩放

        [Header("Auto Update")]
        [Tooltip("Update the reflection probe when there is a significant change in the environment")] public bool isAutoUpdateReflectionProbe = false;//当阳光角度等影响反射球的环境变量发生变化时，更新反射探头
        [Tooltip("The sun's interval rotation angle to update the reflection probe")] [EnableIf(nameof(isAutoUpdateReflectionProbe))] [AllowNesting] [Range(5, 90)] public float updateReflectionProbeIntervalAngle = 30;//更新反射探头的间隔角度
        [Tooltip("The interval time to update the reflection probe")] [EnableIf(nameof(isAutoUpdateReflectionProbe))] [AllowNesting] [Range(1, 90)] public float updateReflectionProbeIntervalTime = 10;//更新反射探头的间隔时间

        [Newtonsoft.Json.JsonConstructor]//Use the specified constructor when deserializing that object（使用该构造函数进行初始化，才能够使用字段的默认值，否则会因为加载旧版配置文件而导致sunDistance为默认值0而报错）
        public ConfigInfo()
        {
        }

        ///ToAdd：
        /// 通过按键更新反射探头
        /// -Fog
    }
    #endregion

}