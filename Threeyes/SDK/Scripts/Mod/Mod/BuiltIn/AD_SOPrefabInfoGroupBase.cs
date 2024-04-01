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
    [ValidateInput(nameof(ValidateTitle), "Title can't be empty!")] public string title;//目录名（UI Bug：首次输入字符后，会因为UI刷新导致焦点丢失）

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

    protected static void CreateUsingSelectedObjectsFunc<TSOInst>(string defaultFileName)
               where TSOInst : AD_SOPrefabInfoGroupBase<TSOPrefabInfo>
    {
#if UNITY_EDITOR
        List<TSOPrefabInfo> listSOPI = new List<TSOPrefabInfo>();

        foreach (Object obj in Selection.objects)
        {
            if (obj is TSOPrefabInfo sOPrefabInfo)
                listSOPI.Add(sOPrefabInfo);
        }
        if (listSOPI.Count == 0)
            return;
        string firstAssetFilePath = AssetDatabase.GetAssetPath(listSOPI[0]);
        string relatedDirPath = EditorPathTool.GetUnityRelateParentPath(firstAssetFilePath);

        ///Todo:
        ///-以第一个有效的物体作为生成文件夹路径
        ///-选中该文件
        string assetPath = relatedDirPath + "/" + defaultFileName + ".asset";
        //bool hasCreated = false;
        TSOInst soInst = AssetDatabase.LoadAssetAtPath<TSOInst>(assetPath);
        if (soInst == null)
        {
            TSOInst soInstTemp = CreateInstance<TSOInst>();
            AssetDatabase.CreateAsset(soInstTemp, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            soInst = AssetDatabase.LoadAssetAtPath<TSOInst>(assetPath);//重新加载Assets中的文件，便于后续被选中
            //hasCreated = true;
        }
        soInst.listData = listSOPI;
        EditorUtility.SetDirty(soInst);
        Selection.objects = new Object[] { soInst };
        //Debug.Log($"{(hasCreated ? "Create" : "Update")}  SOPreabInfoGroup at path: {assetPackPath}");
#endif
    }
}