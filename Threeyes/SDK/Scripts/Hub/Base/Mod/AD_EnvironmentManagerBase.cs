using System.Collections;
using System.Collections.Generic;
using Threeyes.GameFramework;
using UnityEngine;

public class AD_EnvironmentManagerBase<T> : EnvironmentManagerBase<T, IAD_EnvironmentController, AD_DefaultEnvironmentController, IAD_SOEnvironmentControllerConfig>
    , IAD_EnvironmentManager
where T : AD_EnvironmentManagerBase<T>
{
}
