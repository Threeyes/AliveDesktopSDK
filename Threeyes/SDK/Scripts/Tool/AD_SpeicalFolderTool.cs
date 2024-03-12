using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using UnityEngine;
/// <summary>
/// 针对特殊路径的管路工具类
/// </summary>
public static class AD_SpeicalFolderTool
{
     /// <summary>
    /// 打开SpeicalFolder
    /// Ref：
    /// - https://www.codeproject.com/Articles/25983/How-to-Execute-a-Command-in-C
    /// - https://superuser.com/questions/395015/how-to-open-the-recycle-bin-from-the-windows-command-line
    /// </summary>
    /// <param name="specialFolder"></param>
    public static void Open(AD_SpeicalFolder specialFolder)
    {
        if (specialFolder == AD_SpeicalFolder.None)
            return;

        // create the ProcessStartInfo using "cmd" as the program to be run,
        // and "/c " as the parameters.
        // Incidentally, /c tells cmd that we want it to execute the command that follows,
        // and then exit.
        var processStartInfo = new System.Diagnostics.ProcessStartInfo(fileName: "cmd", arguments: "/c " + "start ::" + GetUID(specialFolder));
        // The following commands are needed to redirect the standard output.
        // This means that it will be redirected to the Process.StandardOutput StreamReader.
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.UseShellExecute = false;
        // Do not create the black window.
        processStartInfo.CreateNoWindow = true;
        // Now we create a process, assign its ProcessStartInfo and start it
        var process = System.Diagnostics.Process.Start(processStartInfo);
    }

    /// <summary>
    /// 检查注册表中该图标是否在桌面中可见
    /// Ref：
    /// -BumpTop-BT_WindowsOS.GetIconAvailability
    /// 
    /// PS：
    /// -对应系统设置-主题-桌面图标设置，当用户更改了里面的设置后，就会修改注册表中的特定字段
    /// </summary>
    /// <param name="speicalFolder"></param>
    /// <returns></returns>
    public static bool GetVisibilityOnDesktop(AD_SpeicalFolder speicalFolder)
    {
        bool isVisible = speicalFolder == AD_SpeicalFolder.RecycleBin;//初始化默认值（Recycle bin is on by default, the others are off by default.）

        string registry_NewStartPanelPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
        RegistryKey key = Registry.CurrentUser.OpenSubKey(registry_NewStartPanelPath);//打开路径，如果用户不设置个性化，则该路径不存在。如果该字段不存在，则返回null而不是报错（https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registrykey.opensubkey?view=net-7.0）

        if (key != null)
        {
            string uid = GetUID(speicalFolder);
            if (uid.NotNullOrEmpty())
            {
                object registryValue_SpeicalFolder = key.GetValue(uid);
                if (registryValue_SpeicalFolder != null)
                {
                    if ((int)registryValue_SpeicalFolder == 0)//0代表可见
                        isVisible = true;
                    else//1代表不可见
                        isVisible = false;
                    //Debug.Log("registryValue: " + registryValue_SpeicalFolder);
                }
            }
        }
        key?.Close();
        return isVisible;
    }
    /// <summary>
    /// 获取特殊图标的UID，用于注册表查询
    /// Ref：
    /// - https://www.tweaknow.com/RegTweakHideDesktopIcons.php
    /// - 所有CLSID清单 https://www.tenforums.com/tutorials/3123-clsid-key-guid-shortcuts-list-windows-10-a.html
    /// - Microsoft.WindowsAPICodePack.Shell.FolderIdentifiers（通过字段列出已知特殊文件夹）
    /// </summary>
    /// <param name="speicalFolder"></param>
    /// <returns></returns>
    public static string GetUID(AD_SpeicalFolder speicalFolder)
    {
        //#备注：对应的根注册地址为：计算机\HKEY_CLASSES_ROOT\CLSID\XXX，其中XXX为下面的字符串（带中括号）
        // these values correspond to special icons on the desktop
        switch (speicalFolder)
        {
            case AD_SpeicalFolder.ControlPanel:
                return "{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}";//Control Panel
            case AD_SpeicalFolder.RecycleBin:
                return "{645FF040-5081-101B-9F08-00AA002F954E}";
            case AD_SpeicalFolder.UserProfile:
                return "{59031a47-3f72-44a7-89c5-5595fe6b30ee}";
            case AD_SpeicalFolder.MyComputer:
                return "{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            case AD_SpeicalFolder.Network:
                return "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}";
            default://暂时不算报错
                return "";
        }
    }

    /// <summary>
    /// Ref：KnownFolders
    /// </summary>
    /// <param name="speicalFolder"></param>
    /// <returns></returns>
    public static string GetParsingName(AD_SpeicalFolder speicalFolder)
    {
        switch (speicalFolder)
        {
            case AD_SpeicalFolder.ControlPanel:
                return "::{26EE0668-A00A-44D7-9371-BEB064C98683}\\0";//Warning:后面需要带上序号，主要用于指示呈现的方式，否则ShellHelper会报错
            case AD_SpeicalFolder.RecycleBin:
                return "::{645FF040-5081-101B-9F08-00AA002F954E}";
            case AD_SpeicalFolder.UserProfile:
                return "::{59031a47-3f72-44a7-89c5-5595fe6b30ee}";
            case AD_SpeicalFolder.MyComputer:
                return "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
            case AD_SpeicalFolder.Network:
                return "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}";
            default://暂时不算报错
                return "";
        }
    }
}