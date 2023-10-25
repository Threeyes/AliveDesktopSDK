using System.Collections;
using System.Collections.Generic;
using Threeyes.RuntimeEditor;
using UnityEngine;

public static class AD_ManagerHolder
{
    public static IAD_RuntimeEditorManager RuntimeEditorManager { get; internal set; }


    //——Setting——
    public static IAD_CommonSettingManager CommonSettingManager { get; internal set; }
    //public static IAD_InputManager InputManager { get; internal set; }

    //——Mod——

    public static IAD_XRManager XRManager { get; internal set; }
    public static IAD_FileSystemManager FileSystemManager { get; internal set; }
    public static IAD_DecorationManager DecorationManager { get; internal set; }
    public static IAD_EnvironmentManager EnvironmentManager { get; internal set; }
    public static IAD_PostProcessingManager PostProcessingManager { get; internal set; }
}
