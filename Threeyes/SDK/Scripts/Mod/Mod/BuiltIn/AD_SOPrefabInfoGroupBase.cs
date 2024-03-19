using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using UnityEngine;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
using Threeyes.Core.Editor;
#endif

public abstract class AD_SOPrefabInfoGroupBase<TSOPrefabInfo> : SOGroupBase<TSOPrefabInfo>
    where TSOPrefabInfo : AD_SOPrefabInfo
{
    public string remark;//开发者内部注释
    [Space]
    [ValidateInput(nameof(ValidateTitle), "Title can't be empty!")] public string title;//目录名

    bool ValidateTitle(string value)
    {
        return value.NotNullOrEmpty();
    }
    /// <summary>
    /// 基于自身所在文件夹的SOPrefabInfo进行初始化
    /// </summary>
    protected void InitUsingCurFolderFunc()
    {
#if UNITY_EDITOR
        string absSelfPath = EditorPathTool.GetAssetAbsPath(this);
        string absParentDir = PathTool.GetParentDirectory(absSelfPath).FullName;

        InitUsingDir(absParentDir);
#endif
    }

    /// <summary>
    /// 基于特定文件夹的SOPrefabInfo进行初始化
    /// </summary>
    private void InitUsingDir(string absDir)
    {
#if UNITY_EDITOR
        listData.Clear();
        string relatedParentDir = EditorPathTool.AbsToUnityRelatePath(absDir);

        foreach (TSOPrefabInfo soPI in AssetDatabaseTool.LoadAssets<TSOPrefabInfo>("t:ScriptableObject", new string[] { relatedParentDir }))
        {
            listData.AddOnce(soPI);
        }
        EditorUtility.SetDirty(this);
#endif
    }
}