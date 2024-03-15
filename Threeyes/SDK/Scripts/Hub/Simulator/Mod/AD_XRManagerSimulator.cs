using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// ToAdd:
/// -PC模式：激活PC模拟器
/// </summary>
public class AD_XRManagerSimulator : AD_XRManagerBase<AD_XRManagerSimulator>
{
    public GameObject goXRDeviceSimulator;//XR Device Simulator的根物体
    protected override void SetInstanceFunc()
    {
        base.SetInstanceFunc();
        Init(); //初始化VR模式等
    }

    protected override void InitPlatform()
    {
        AD_PlatformMode platformMode = AD_ManagerHolderManager.ActivePlatformMode;
        goXRDeviceSimulator.SetActive(platformMode == AD_PlatformMode.PC);//XRDeviceSimulator会在OnEnable时，自动移除其他VR设备

        //如果OpenXR没有设置为InitializeXRonStartup，则需要手动调用此方法来初始化OpenXR
        if (platformMode == AD_PlatformMode.PCVR)
        {
            StartXR();
        }
    }
}
