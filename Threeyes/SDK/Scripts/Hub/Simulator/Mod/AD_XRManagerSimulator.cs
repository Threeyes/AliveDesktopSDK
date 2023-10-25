using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AD_XRManagerSimulator : AD_XRManagerBase<AD_XRManagerSimulator>
{
    protected override void SetInstanceFunc()
    {
        base.SetInstanceFunc();
        Init(); //初始化VR模式等
    }
}
