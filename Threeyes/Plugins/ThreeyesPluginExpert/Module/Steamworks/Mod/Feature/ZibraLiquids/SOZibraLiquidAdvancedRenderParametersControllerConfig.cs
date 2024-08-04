#if USE_ZibraLiquid
#if !UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using Threeyes.Config;
using UnityEngine;

namespace Threeyes.Steamworks
{
    [CreateAssetMenu(menuName = Steamworks_EditorDefinition.AssetMenuPrefix_Root_Feature + "ZibraLiquid/AdvancedRenderParametersController", fileName = "ZibraLiquidAdvancedRenderParametersControllerConfig")]
    public class SOZibraLiquidAdvancedRenderParametersControllerConfig : SOConfigBase<ZibraLiquidAdvancedRenderParametersController.ConfigInfo> { }
}
#endif
#endif