using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Threeyes.GameFramework;

public class AD_PostProcessingManagerBase<T> : PostProcessingManagerBase<T, IAD_PostProcessingController, AD_DefaultPostProcessingController, IAD_SOPostProcessingControllerConfig>
    , IAD_PostProcessingManager
where T : AD_PostProcessingManagerBase<T>
{
}
