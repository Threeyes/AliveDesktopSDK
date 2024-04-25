using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Threeyes.Steamworks;
public interface IAD_EnvironmentController : IEnvironmentController
{
    void RegisterCustomSunEntity(AD_SunEntityController customSunEntityController);
    void UnRegisterCustomSunEntity(AD_SunEntityController customSunEntityController);
}
