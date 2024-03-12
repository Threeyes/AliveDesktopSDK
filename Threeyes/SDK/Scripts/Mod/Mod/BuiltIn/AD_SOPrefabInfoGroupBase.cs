using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using UnityEngine;

public abstract class AD_SOPrefabInfoGroupBase<TSOPrefabInfo> : SOGroupBase<TSOPrefabInfo>
{
    public string remark;//开发者内部注释
    [Space]
    public string title;//目录名
}