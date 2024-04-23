using NaughtyAttributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Threeyes.Config;
using Threeyes.Core;
using Threeyes.Persistent;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using DefaultValue = System.ComponentModel.DefaultValueAttribute;

namespace Threeyes.Steamworks
{
    /// <summary>
    /// Control Environment Setting
    /// 
    /// PS:
    /// 1.Default Environment Lighting/Reflections Sources come from Skybox, inheric this class if your want to change them
    /// 2.该类只是提供常见的实现，不一定需要继承该类，使用父类也可
    /// </summary>
    //[AddComponentMenu(Steamworks_EditorDefinition.ComponentMenuPrefix_Root_Mod_Controller + "DefaultEnvironmentController")]
    public class DefaultEnvironmentController<TSOConfig, TConfig> : EnvironmentControllerBase<TSOConfig, TConfig>
       where TSOConfig : SOConfigBase<TConfig>, ISOEnvironmentControllerConfig
        where TConfig : DefaultEnvironmentControllerConfigInfo
    {
        #region Property & Field
        //PS：以下是场景相关的配置，暂不需要通过EnableIf来激活
        [Header("Lights")]
        [Tooltip("The Root gameobject for all lights")] [Required] [SerializeField] protected GameObject goLightGroup;
        [Tooltip("When the Skybox Material is a Procedural Skybox, use this setting to specify a GameObject with a directional Light component to indicate the direction of the sun (or whatever large, distant light source is illuminating your Scene). If this is set to None, the brightest directional light in the Scene is assumed to represent the sun. Lights whose Render Mode property is set to Not Important do not affect the Skybox.")] [Required] [SerializeField] protected Light sunSourceLight;//(Can be null)

        [Header("Reflection")]
        [Tooltip("The main ReflectionProbe")] [Required] [SerializeField] protected ReflectionProbe reflectionProbe;

        #endregion

        #region Unity Method
        protected virtual void Awake()
        {
            Config.actionIsUseLightsChanged += OnIsUseLightsChanged;
            Config.actionIsUseReflectionChanged += OnIsUseReflectionChanged;
            Config.actionIsUseSkyboxChanged += OnIsUseSkyboxChanged;
            Config.actionPersistentChanged += OnPersistentChanged;//Get called at last
        }
        protected virtual void OnDestroy()
        {
            Config.actionIsUseLightsChanged -= OnIsUseLightsChanged;
            Config.actionIsUseReflectionChanged -= OnIsUseReflectionChanged;
            Config.actionIsUseSkyboxChanged -= OnIsUseSkyboxChanged;
            Config.actionPersistentChanged -= OnPersistentChanged;
        }
        #endregion

        #region Config Callback
        public override void OnModControllerInit()
        {
            UpdateSetting();
        }
        void OnIsUseLightsChanged(PersistentChangeState persistentChangeState)
        {
        }
        void OnIsUseReflectionChanged(PersistentChangeState persistentChangeState)
        {
        }
        void OnIsUseSkyboxChanged(PersistentChangeState persistentChangeState)
        {
        }
        void OnPersistentChanged(PersistentChangeState persistentChangeState)
        {
            UpdateSetting();
        }

        protected virtual void UpdateSetting()
        {
            SetLightsActive(Config.isUseLights);
            SetReflectionProbeActive(Config.isUseReflection);//Update ReflectionProbe's gameobject active state before skybox changes, or else the render may not update property
            SetSkyboxActive(Config.isUseSkybox);
        }
        #endregion

        #region Module Setting
        bool lastReflectionProbeUsed = false;//Cache state, avoid render multi times
        bool lastSkyboxUsed = false;//Cache state, avoid render multi times
        protected virtual void SetLightsActive(bool isUse)
        {
            goLightGroup?.SetActive(isUse);
            if (isUse)
            {
                RenderSettings.sun = sunSourceLight;
                if (sunSourceLight)
                {
                    sunSourceLight.transform.eulerAngles = Config.sunLightRotation;
                    sunSourceLight.intensity = Config.sunLightIntensity;
                    sunSourceLight.color = Config.sunLightColor;
                    sunSourceLight.shadows = Config.lightShadowType;
                }
            }
        }
        protected virtual void SetReflectionProbeActive(bool isUse)
        {
            if (!reflectionProbe)
                return;
            bool activeStateChanged = lastReflectionProbeUsed != isUse;
            lastReflectionProbeUsed = isUse;
            reflectionProbe.gameObject.SetActive(isUse);
            if (isUse && activeStateChanged)//在重新激活时要重新刷新
                RefreshReflectionProbe();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isUse"></param>
        /// <returns>If skybox changed</returns>
        protected virtual void SetSkyboxActive(bool isUse)
        {
            bool needRefresh = lastSkyboxUsed != isUse;
            lastSkyboxUsed = isUse;
            if (isUse)//使用：尝试更新参数
            {
                //#1 尝试更新参数（不管PanoramaSkybox材质是否在用，都要设置）
                needRefresh |= TrySetPanoramaSkybox_Texture();//update texture first
                needRefresh |= TrySetPanoramaSkybox_Rotation();

                //#2 尝试更新全局skybox材质
                needRefresh |= TrySetSkybox();
            }
            else//不使用任意Skybox：清空Skybox设置
            {
                needRefresh = TrySetSkyboxFunc(null);//设置为null
            }
            if (needRefresh)//修改天空盒后：更新GI（如反射探头）
                DynamicGIUpdateEnvironment();
        }


        protected bool TrySetSkyboxFunc(Material material)
        {
            //PS：不检查material是否为null，因为null可用于清空
            if (RenderSettings.skybox != material)
            {
                RenderSettings.skybox = material;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="texture"></param>
        /// <returns>Skybox texture changed</returns>
        bool TrySetPanoramaSkybox_Texture()
        {
            string panoMatTextureName = "_MainTex";
            if (Config.panoramaSkyboxMaterial && Config.panoramaSkyboxMaterial.HasTexture(panoMatTextureName)/* && Config.PanoramaSkyboxTexture*/)//不管有无贴图，都需要更新，便于重置
            {
                if (Config.panoramaSkyboxMaterial.GetTexture(panoMatTextureName) != Config.PanoramaSkyboxTexture)//仅当贴图不同才更新
                {
                    Config.panoramaSkyboxMaterial.SetTexture(panoMatTextureName, Config.PanoramaSkyboxTexture);
                    return true;
                }
            }
            return false;
        }
        bool TrySetPanoramaSkybox_Rotation()
        {
            string panoMatRotationName = "_Rotation";
            if (Config.panoramaSkyboxMaterial && Config.panoramaSkyboxMaterial.HasFloat(panoMatRotationName) && Config.PanoramaSkyboxTexture)
            {
                if (Config.panoramaSkyboxMaterial.GetFloat(panoMatRotationName) != Config.panoramaSkyboxRotation)
                {
                    Config.panoramaSkyboxMaterial.SetFloat(panoMatRotationName, Config.panoramaSkyboxRotation);
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region CustomSkybox
        public override int CustomSkyboxCount { get { return listCustomSkyboxController.Count; } }//方便检查当前自定义天空盒的数量
        List<SkyboxController> listCustomSkyboxController = new List<SkyboxController>();
        public override void RegisterCustomSkybox(SkyboxController skyboxController)
        {
            listCustomSkyboxController.AddOnce(skyboxController);
            listCustomSkyboxController.Remove(null);//移除可能因场景切换等情况被删除的实例
            if (!Config.isUseSkybox)
                return;

            //更新天空盒设置TrySetSkybox，如果有更新则调用DynamicGIUpdateEnvironment
            bool needRefresh = TrySetSkybox();
            if (needRefresh)
                DynamicGIUpdateEnvironment();
        }
        public override void UnRegisterCustomSkybox(SkyboxController skyboxController)
        {
            if (listCustomSkyboxController.Count > 0)
            {
                listCustomSkyboxController.Remove(skyboxController);
                listCustomSkyboxController.Remove(null);//移除可能被删除的实例
            }

            if (!Config.isUseSkybox)//控制全局是否使用Skybox（包括Custom）
                return;

            bool needRefresh = TrySetSkybox();
            if (needRefresh)
                DynamicGIUpdateEnvironment();
        }

        /// <summary>
        /// 会自动使用首个有效的Skybox
        /// </summary>
        bool TrySetSkybox()
        {
            Material targetMaterial = Config.SkyboxMaterial;//默认使用Config的配置，避免listCustomSkyboxController中无有效元素
            if (listCustomSkyboxController.Count > 0)//如果有自定义的Skybox，则优先使用
            {
                SkyboxController lastSC = listCustomSkyboxController.LastOrDefault();//获取最后加入的天空盒
                if (lastSC != null)
                    targetMaterial = lastSC.skyboxMaterial;
            }
            return TrySetSkyboxFunc(targetMaterial);
        }
        #endregion

        #region Utility
        /// <summary>
        /// Schedules an update of the environment cubemap.
        /// （更新GI及反射）
        /// 
        /// Warning: Expensive operation! Only call it when the skybox changed
        /// 
        /// Ref: Changing the skybox at runtime will not update the ambient lighting automatically. You need to call DynamicGI.UpdateEnvironment() to let the engine know you want to update the ambient lighting. 
        /// Warning: This is a relatively expensive operation, which is why it’s not done automatically while the game is running.
        /// (https://forum.unity.com/threads/changing-skybox-materials-via-script.544854/#:~:text=Changing%20the%20skybox%20at%20runtime%20will%20not%20update,not%20done%20automatically%20while%20the%20game%20is%20running.)
        /// 
        /// PS:因为刷新GI会有损耗，因此只有当Skybox材质有变化时才调用该方法（注意不会更新Reflection，需要用户自行使用Reflection Probe实现）
        /// </summary>
        protected virtual void DynamicGIUpdateEnvironment()
        {
            RuntimeTool.ExecuteOnceInCurFrameAsync(DynamicGI.UpdateEnvironment);//Update environment cubemap
            RefreshReflectionProbe();
        }

        protected float lastUpdateReflectionProbeTime = 0;
        [ContextMenu("RefreshReflectionProbe")]
        /// <summary>
        /// Update ReflectionProbe to refresh reflection
        /// </summary>
        protected bool RefreshReflectionProbe()
        {
            if (!reflectionProbe)
                return false;
            if (!lastReflectionProbeUsed)//PS:未激活时调用无效
                return false;


            //public enum ReflectionProbeMode
            //{
            //    Baked,//Reflection probe is baked in the Editor.
            //    Realtime,//Reflection probe is updating in real-time.
            //    Custom//Reflection probe uses a custom texture specified by the user.
            //}

            //仅当反射探头的属性为Realtime及ViaScripting时，才能调用方法更新
            if (reflectionProbe.mode == ReflectionProbeMode.Realtime && reflectionProbe.refreshMode == ReflectionProbeRefreshMode.ViaScripting)
            {
                //确保同一帧只调用一次
                RuntimeTool.ExecuteOnceInCurFrameAsync(() =>
                {
                    reflectionProbe.RenderProbe(/*reflectionProbe.realtimeTexture*/);//PS:RenderProbe会返回ID，可用于后续检查Render完成时间

                });
                lastUpdateReflectionProbeTime = Time.time;//记录渲染时间
                return true;
            }
            return false;
        }
        #endregion
    }

    #region Define
    /// <summary>
    ///
    ///
    /// Note:
    /// 1.在1.1.4版本中，isUseLights及isUsePanoramicSkybox经过了重命名，因此需要使用[DefaultValue]及[JsonProperty]指定默认值，避免Json中无对应字段而使用false作为初始值
    /// 2.为了方便ConfigInfo的命名，该类不加Base后缀（以后都按此规则）
    /// </summary>
    [Serializable]
    [PersistentChanged(nameof(DefaultEnvironmentControllerConfigInfo.OnPersistentChanged))]
    public class DefaultEnvironmentControllerConfigInfo : SerializableDataBase
    {
        [JsonIgnore] public UnityAction<PersistentChangeState> actionIsUseReflectionChanged;
        [JsonIgnore] public UnityAction<PersistentChangeState> actionIsUseLightsChanged;
        [JsonIgnore] public UnityAction<PersistentChangeState> actionIsUseSkyboxChanged;
        [JsonIgnore] public UnityAction<PersistentChangeState> actionPersistentChanged;

        public Material SkyboxMaterial { get { return skyboxType == SkyboxType.Default ? defaultSkyboxMaterial : panoramaSkyboxMaterial; } }
        public Texture PanoramaSkyboxTexture { get { return externalPanoramaTexture ? externalPanoramaTexture : defaultPanoramaTexture; } }

        [Header("Lights")]
        [PersistentValueChanged(nameof(OnPersistentValueChanged_IsUseLights))] public bool isUseLights = true;
        [EnableIf(nameof(isUseLights))] [AllowNesting] public Vector3 sunLightRotation = new Vector3(30, 30, 240);
        [EnableIf(nameof(isUseLights))] [AllowNesting] [Range(0, 8)] public float sunLightIntensity = 0.3f;
        [EnableIf(nameof(isUseLights))] [AllowNesting] public Color sunLightColor = Color.white;
        [EnableIf(nameof(isUseLights))] public LightShadows lightShadowType = LightShadows.None;

        [Header("ReflectionProbe")]
        [PersistentValueChanged(nameof(OnPersistentValueChanged_IsUseReflection))] public bool isUseReflection = true;

        [Header("Skybox")]//Skybox的材质参数不一致，仅提供最通用字段，其他后续通过SkyboxController+MaterialController进行自定义
        [DefaultValue(true)] [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)] [PersistentValueChanged(nameof(OnPersistentValueChanged_IsUseSkybox))] public bool isUseSkybox = true;//控制全局是否使用Skybox（包括Custom）
        [EnableIf(nameof(isUseSkybox))] public SkyboxType skyboxType = SkyboxType.Default;
        //Default
        [ValidateInput(nameof(ValidateDefaultSkyboxMaterial), "The defaultSkyboxMaterial's shader should be the one in \"Skybox/...\" catelogy")] [EnableIf(nameof(isUseDefaultSkybox))] [AllowNesting] [JsonIgnore] public Material defaultSkyboxMaterial;
        //Panorama  
        [ValidateInput(nameof(ValidatePanoramaSkyboxMaterial), "The panoramaSkyboxMaterial's shader should be the one in \"Skybox/...\" catelogy")] [EnableIf(nameof(isUsePanoramicSkybox))] [AllowNesting] [JsonIgnore] public Material panoramaSkyboxMaterial;
        ///Skybox/Panoramic Shader中的全景图。（PS：Panorama类型的图片不要选中 "generate mipmaps"，否则会产生缝（外部加载的图片默认都不会生成））
        [EnableIf(nameof(isUsePanoramicSkybox))] [AllowNesting] [JsonIgnore] public Texture defaultPanoramaTexture;
        [EnableIf(nameof(isUsePanoramicSkybox))] [ReadOnly] [AllowNesting] [JsonIgnore] public Texture externalPanoramaTexture;
        [EnableIf(nameof(isUsePanoramicSkybox))] [AllowNesting] [PersistentAssetFilePath(nameof(externalPanoramaTexture), true)] public string externalPanoramaTextureFilePath;
        [EnableIf(nameof(isUsePanoramicSkybox))] [AllowNesting] [Range(0, 360)] public float panoramaSkyboxRotation = 0;
        [HideInInspector] [JsonIgnore] [PersistentDirPath] public string PersistentDirPath;

        #region Callback
        void OnPersistentValueChanged_IsUseReflection(PersistentChangeState persistentChangeState)
        {
            actionIsUseReflectionChanged.Execute(persistentChangeState);
        }
        void OnPersistentValueChanged_IsUseLights(PersistentChangeState persistentChangeState)
        {
            actionIsUseLightsChanged.Execute(persistentChangeState);
        }
        void OnPersistentValueChanged_IsUseSkybox(PersistentChangeState persistentChangeState)
        {
            actionIsUseSkyboxChanged.Execute(persistentChangeState);
        }

        void OnPersistentChanged(PersistentChangeState persistentChangeState)
        {
            actionPersistentChanged.Execute(persistentChangeState);
        }
        #endregion

        #region NaughtAttribute
        bool isUseDefaultSkybox { get { return isUseSkybox && skyboxType == SkyboxType.Default; } }
        bool isUsePanoramicSkybox { get { return isUseSkybox && skyboxType == SkyboxType.Panoramic; } }
        //PS:用户可能会自定义SkyboxShader，同时系统会自动判断材质是否有效，而且其他类型的Shader也能用，因此不需要判断是否使用了Skybox类型Shader（仅作为提示，不限制使用或打包）
        bool ValidateDefaultSkyboxMaterial(Material material)
        {
            if (material)
            {
                return material.shader.name.StartsWith("Skybox");
            }
            return true;//值为空不作错误处理
        }
        bool ValidatePanoramaSkyboxMaterial(Material material)
        {
            //string panoramaSkyboxShaderName = "Skybox/Panoramic";//PS:不限定，便于用户自行实现shader
            if (material)
            {
                return material.shader.name.StartsWith("Skybox");
            }
            return true;//值为空不作错误处理
        }
        #endregion

        #region Define
        public enum SkyboxType
        {
            Default,
            Panoramic
        }
        //PS:useColorTemperature等参数并不通用，为减少复杂度，让用户自行实现
        #endregion
    }
    #endregion
}