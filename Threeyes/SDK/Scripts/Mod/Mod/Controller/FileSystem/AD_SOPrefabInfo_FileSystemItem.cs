using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
/// <summary>
/// Todo:
/// -当程序初始化时，优先从库中查找首个符合条件（如SpeicalFolder）的Item，如果没有才使用通用图标；
/// -当用户需要更改图标时，提供符合条件的图标以及通用图标；
/// -可以多选并批量更换样式
/// </summary>
[CreateAssetMenu(menuName = AD_EditorDefinition.AssetMenuPrefix_Root + "PrefabInfo/FileSystemItem", fileName = "FileSystemItem")]
public class AD_SOPrefabInfo_FileSystemItem : AD_SOPrefabInfoBase
{
    //——Condition——
    /// <summary>
    /// Set the corresponding shell type
    /// (ToUse)
    /// </summary>
    public AD_ShellType shellType = AD_ShellType.File;
    /// Set this field if prefab has corresponding special folder
    /// </summary>
    [EnableIf(nameof(shellType), AD_ShellType.SpecialFolder)] public AD_SpeicalFolder speicalFolder = AD_SpeicalFolder.None;
}