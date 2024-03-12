using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// 管理运行/模拟时的命令行
/// </summary>
public static class AD_CommandLineManager
{
    const string commandArg_VRMode = "-vrMode";
    const string commandArg_WindowMode = "-window";
    const string commandArg_FullScreenMode = "-fullScreen";
    const string commandArg_ScreenWidth = "-screen-width";//Unity的参数
    const string commandArg_ScreenHeight = "-screen-height";//Unity的参数


    //Runtime
    static bool hasInited = false;
    static List<string> listCommand = new List<string>();
    static int overrideScreenWidth = -1;
    static int overrideScreenHeight = -1;
    public static void TryInit()
    {
        if (!hasInited)
        {
            listCommand = Environment.GetCommandLineArgs().ToList();
            int i = 0;
            while (i < listCommand.Count)
            {
                string arg = listCommand[i];

                if (arg == commandArg_ScreenWidth)//screen width
                {
                    i++;
                    if (i < listCommand.Count)
                    {
                        if (int.TryParse(listCommand[i], out overrideScreenWidth))
                        {
                            Debug.LogError($"Read command {nameof(overrideScreenWidth)}={overrideScreenWidth}");
                        }
                    }
                }
                if (arg == commandArg_ScreenHeight)//screen height
                {
                    i++;
                    if (i < listCommand.Count)
                    {
                        if (int.TryParse(listCommand[i], out overrideScreenHeight))
                        {
                            Debug.LogError($"Read command {nameof(overrideScreenHeight)}={overrideScreenHeight}");
                        }
                    }
                }

                i++;//下一个
            }

            hasInited = true;
        }
    }

    public static bool IsVRMode
    {
        get
        {
#if UNITY_EDITOR
            if (Application.isEditor)//【编辑器模式】读取Resource的配置并初始化
            {
                return AD_SOEditorSettingManager.Instance.PlatformMode == AD_PlatformMode.PCVR;
            }
            else
#endif
            {
                TryInit();
                return listCommand.Contains(commandArg_VRMode);
            }
        }
    }

    //——Build Only——
    public static bool IsWindowMode
    {
        get
        {
            //PS：仅真机有效，所以不需要模拟
            TryInit();
            return listCommand.Contains(commandArg_WindowMode);
        }
    }
    public static bool IsFullScreenMode
    {
        get
        {
            //PS：仅真机有效，所以不需要模拟
            TryInit();
            return listCommand.Contains(commandArg_FullScreenMode);
        }
    }

    /// <summary>
    /// -1代表无效
    /// </summary>
    public static int OverrideScreenWidth
    {
        get
        {
            TryInit();
            return overrideScreenWidth;
        }
    }
    public static int OverrideScreenHeight
    {
        get
        {
            TryInit();
            return overrideScreenHeight;
        }
    }
}
