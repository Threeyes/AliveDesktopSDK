using NaughtyAttributes;
using Newtonsoft.Json;
using Threeyes.Persistent;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
/// <summary>
///Control PostProcessing Setting
///
/// Note:
/// 1.只提供最常见且URP可用的Effect，如有特定需求，请重新定义一个Controller
/// </summary>

[AddComponentMenu(AD_EditorDefinition.ComponentMenuPrefix_Root_Mod_Controller + "AD_DefaultPostProcessingController")]
public partial class AD_DefaultPostProcessingController : PostProcessingControllerBase<AD_SODefaultPostProcessingControllerConfig, AD_DefaultPostProcessingController.ConfigInfo>, IAD_PostProcessingController
{
    public override bool IsUsePostProcessing { get { return Config.isUsePostProcessing; } }

    [Header("PostProcessing")]
    [Tooltip("The PostProcessing volume")] [Required] [SerializeField] protected Volume volume;

    #region Unity Method
    private void Awake()
    {
        Config.actionPersistentChanged += OnPersistentChanged;//Get called at last
    }
    private void OnDestroy()
    {
        Config.actionPersistentChanged -= OnPersistentChanged;
    }
    #endregion

    #region Config Callback
    void OnPersistentChanged(PersistentChangeState persistentChangeState)
    {
        UpdateSetting(Config.isUsePostProcessing);
    }
    #endregion

    #region Override
    Bloom bloom = null;
    ChannelMixer channelMixer = null;
    ChromaticAberration chromaticAberration = null;
    ColorAdjustments colorAdjustments = null;
    //ColorCurves colorCurves = null;
    ColorLookup colorLookup = null;
    DepthOfField depthOfField = null;
    FilmGrain filmGrain = null;
    LensDistortion lensDistortion = null;
    LiftGammaGain liftGammaGain = null;
    MotionBlur motionBlur = null;
    PaniniProjection paniniProjection = null;
    ShadowsMidtonesHighlights shadowsMidtonesHighlights = null;
    SplitToning splitToning = null;
    Tonemapping tonemapping = null;
    Vignette vignette = null;
    WhiteBalance whiteBalance = null;
    public override void UpdateSetting(bool isUse)
    {
        if (!volume)
            return;
        if (!volume.profile)
            return;

        volume.gameObject.SetActive(isUse);//控制整体激活状态

        ///ToUpdate:
        ///-【V2】如果相关Effect没激活，那就直接移除（尝试 volume.profile.Remove）。需要比较一下性能是否比保留Effect更好，因为需要实现移除、比对等额外操作，如没必要就不实现

        //——Bloom——
        if (volume.profile.TryGet(out bloom))
        {
            bloom.active = Config.bloom_IsActive;
            bloom.threshold.value = Config.bloom_Threshold;
            bloom.intensity.value = Config.bloom_Intensity;
            bloom.scatter.value = Config.bloom_Scatter;
            bloom.clamp.value = Config.bloom_Clamp;
            bloom.tint.value = Config.bloom_Tint;

            bloom.dirtIntensity.value = Config.bloom_DirtIntensity;
        }
        if (volume.profile.TryGet(out channelMixer))
        {
            channelMixer.active = Config.channelMixer_IsActive;
            channelMixer.redOutRedIn.value = Config.channelMixer_RedOutRedIn;
            channelMixer.redOutGreenIn.value = Config.channelMixer_RedOutGreenIn;
            channelMixer.redOutBlueIn.value = Config.channelMixer_RedOutBlueIn;
            channelMixer.greenOutRedIn.value = Config.channelMixer_GreenOutRedIn;
            channelMixer.greenOutGreenIn.value = Config.channelMixer_GreenOutGreenIn;
            channelMixer.greenOutBlueIn.value = Config.channelMixer_GreenOutBlueIn;
            channelMixer.blueOutRedIn.value = Config.channelMixer_BlueOutRedIn;
            channelMixer.blueOutGreenIn.value = Config.channelMixer_BlueOutGreenIn;
            channelMixer.blueOutBlueIn.value = Config.channelMixer_BlueOutBlueIn;
        }
        if (volume.profile.TryGet(out chromaticAberration))
        {
            chromaticAberration.active = Config.chromaticAberration_IsActive;
            chromaticAberration.intensity.value = Config.chromaticAberration_Intensity;
        }
        if (volume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments.active = Config.colorAdjustments_IsActive;
            colorAdjustments.postExposure.value = Config.colorAdjustments_PostExposure;
            colorAdjustments.contrast.value = Config.colorAdjustments_Contrast;
            colorAdjustments.colorFilter.value = Config.colorAdjustments_ColorFilter;
            colorAdjustments.hueShift.value = Config.colorAdjustments_HueShift;
            colorAdjustments.saturation.value = Config.colorAdjustments_Saturation;
        }
        if (volume.profile.TryGet(out colorLookup))
        {
            colorLookup.active = Config.colorLookup_IsActive;
            colorLookup.contribution.value = Config.colorAdjustments_Contribution;
        }
        if (volume.profile.TryGet(out depthOfField))
        {
            depthOfField.active = Config.depthOfField_IsActive;
            depthOfField.mode.value = Config.depthOfField_Mode;
            depthOfField.gaussianStart.value = Config.depthOfField_GaussianStart;
            depthOfField.gaussianEnd.value = Config.depthOfField_GaussianEnd;
            depthOfField.gaussianMaxRadius.value = Config.depthOfField_GaussianMaxRadius;
            depthOfField.highQualitySampling.value = Config.depthOfField_HighQualitySampling;
            depthOfField.focusDistance.value = Config.depthOfField_FocusDistance;
            depthOfField.aperture.value = Config.depthOfField_Aperture;
            depthOfField.focalLength.value = Config.depthOfField_FocalLength;
            depthOfField.bladeCount.value = Config.depthOfField_BladeCount;
            depthOfField.bladeCurvature.value = Config.depthOfField_BladeCurvature;
            depthOfField.bladeRotation.value = Config.depthOfField_BladeRotation;
        }
        if (volume.profile.TryGet(out filmGrain))
        {
            filmGrain.active = Config.filmGrain_IsActive;
            filmGrain.type.value = Config.filmGrain_Type;
            filmGrain.intensity.value = Config.filmGrain_Intensity;
            filmGrain.response.value = Config.filmGrain_Response;
        }
        if (volume.profile.TryGet(out lensDistortion))
        {
            lensDistortion.active = Config.lensDistortion_IsActive;
            lensDistortion.intensity.value = Config.lensDistortion_Intensity;
            lensDistortion.xMultiplier.value = Config.lensDistortion_XMultiplier;
            lensDistortion.yMultiplier.value = Config.lensDistortion_YMultiplier;
            lensDistortion.center.value = Config.lensDistortion_Center;
            lensDistortion.scale.value = Config.lensDistortion_Scale;
        }
        if (volume.profile.TryGet(out liftGammaGain))
        {
            liftGammaGain.active = Config.liftGammaGain_IsActive;
            liftGammaGain.lift.value = Config.liftGammaGain_Lift;
            liftGammaGain.gamma.value = Config.liftGammaGain_Gamma;
            liftGammaGain.gain.value = Config.liftGammaGain_Gain;
        }
        if (volume.profile.TryGet(out motionBlur))
        {
            motionBlur.active = Config.motionBlur_IsActive;
            motionBlur.mode.value = Config.motionBlur_Mode;
            motionBlur.quality.value = Config.motionBlur_Quality;
            motionBlur.intensity.value = Config.motionBlur_Intensity;
            motionBlur.clamp.value = Config.motionBlur_Clamp;
        }
        if (volume.profile.TryGet(out paniniProjection))
        {
            paniniProjection.active = Config.paniniProjection_IsActive;
            paniniProjection.distance.value = Config.paniniProjection_Distance;
            paniniProjection.cropToFit.value = Config.paniniProjection_CropToFit;
        }
        if (volume.profile.TryGet(out shadowsMidtonesHighlights))
        {
            shadowsMidtonesHighlights.active = Config.shadowsMidtonesHighlights_IsActive;
            shadowsMidtonesHighlights.shadows.value = Config.shadowsMidtonesHighlights_Shadows;
            shadowsMidtonesHighlights.midtones.value = Config.shadowsMidtonesHighlights_Midtones;
            shadowsMidtonesHighlights.highlights.value = Config.shadowsMidtonesHighlights_Highlights;
            shadowsMidtonesHighlights.shadowsStart.value = Config.shadowsMidtonesHighlights_ShadowsStart;
            shadowsMidtonesHighlights.shadowsEnd.value = Config.shadowsMidtonesHighlights_ShadowsEnd;
            shadowsMidtonesHighlights.highlightsStart.value = Config.shadowsMidtonesHighlights_ShadowsStart;
            shadowsMidtonesHighlights.highlightsEnd.value = Config.shadowsMidtonesHighlights_HighlightsEnd;
        }
        if (volume.profile.TryGet(out splitToning))
        {
            splitToning.active = Config.splitToning_IsActive;
            splitToning.shadows.value = Config.splitToning_Shadows;
            splitToning.highlights.value = Config.splitToning_Highlights;
            splitToning.balance.value = Config.splitToning_Balance;
        }
        if (volume.profile.TryGet(out tonemapping))
        {
            tonemapping.active = Config.tonemapping_IsActive;
            tonemapping.mode.value = Config.tonemapping_Mode;
        }
        if (volume.profile.TryGet(out vignette))
        {
            vignette.active = Config.vignette_IsActive;
            vignette.color.value = Config.vignette_Color;
            vignette.center.value = Config.vignette_Center;
            vignette.intensity.value = Config.vignette_Intensity;
            vignette.smoothness.value = Config.vignette_Smoothness;
            vignette.rounded.value = Config.vignette_Rounded;
        }
        if (volume.profile.TryGet(out whiteBalance))
        {
            whiteBalance.active = Config.whiteBalance_IsActive;
            whiteBalance.temperature.value = Config.whiteBalance_Temperature;
            whiteBalance.tint.value = Config.whiteBalance_Tint;
        }

        base.UpdateSetting(isUse);//Notify Hub Manager to update camera setting
    }
    #endregion

    #region Editor Method
#if UNITY_EDITOR
    //——MenuItem——
    static string instName = "DefaultPostProcessingController";
    [UnityEditor.MenuItem(AD_EditorDefinition.HierarchyMenuPrefix_Root_Mod_Controller_PostProcessing + "Default", false)]
    public static void CreateInst()
    {
        Threeyes.Editor.EditorTool.CreateGameObjectAsChild<AD_DefaultPostProcessingController>(instName);
    }
#endif
    #endregion
}
