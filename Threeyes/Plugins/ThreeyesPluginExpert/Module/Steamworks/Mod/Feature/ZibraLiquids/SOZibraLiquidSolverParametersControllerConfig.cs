#if USE_ZibraLiquid
#if !UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using Threeyes.Config;
using UnityEngine;

namespace Threeyes.Steamworks
{
    [CreateAssetMenu(menuName = Steamworks_EditorDefinition.AssetMenuPrefix_Root_Feature + "ZibraLiquid/SolverParametersController", fileName = "ZibraLiquidSolverParametersControllerConfig")]
    public class SOZibraLiquidSolverParametersControllerConfig : SOConfigBase<ZibraLiquidSolverParametersController.ConfigInfo> { }
}
#endif
#endif