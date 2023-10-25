using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AD_PhysicsSpeicalFolderTool
{
    /// <summary>
    /// 获取对应的路径
    /// </summary>
    /// <param name="physicsSpeicalFolder"></param>
    /// <param name="cursomDir"></param>
    /// <returns></returns>
    public static List<string> GetFolderPaths(AD_PhysicsSpeicalFolder physicsSpeicalFolder, string cursomDir = "")
    {
        List<string> listDirPath = new List<string>();
        switch (physicsSpeicalFolder)
        {
            case AD_PhysicsSpeicalFolder.Custom:
                listDirPath.Add(cursomDir);
                break;
            case AD_PhysicsSpeicalFolder.Desktop:
                ///【注意】桌面显示以下两个文件夹的文件，因此都要扫描(https://superuser.com/questions/596729/desktop-folder-not-showing-all-the-shortcuts-i-have-on-my-desktop)：
                listDirPath.Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));//【个人路径】C:\Users\[UserName]\Desktop
                listDirPath.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));//【公用路径】C:\Users\Public\Desktop
                break;
            case AD_PhysicsSpeicalFolder.MyDocuments:
                listDirPath.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                break;
            case AD_PhysicsSpeicalFolder.MyMusic:
                listDirPath.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
                break;
            case AD_PhysicsSpeicalFolder.MyVideos:
                listDirPath.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
                break;
            case AD_PhysicsSpeicalFolder.MyPictures:
                listDirPath.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
                break;
        }
        return listDirPath;
    }
}
