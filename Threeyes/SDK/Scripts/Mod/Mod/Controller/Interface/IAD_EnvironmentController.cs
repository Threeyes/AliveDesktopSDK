using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Threeyes.Steamworks;
public interface IAD_EnvironmentController : IEnvironmentController
{
    AD_SunEntityController ActiveSunEntityController { get; }
    int SunEntityControllerCount { get; }
    void RegisterSunEntityController(AD_SunEntityController customSunEntityController);
    void UnRegisterSunEntityController(AD_SunEntityController customSunEntityController);
}
