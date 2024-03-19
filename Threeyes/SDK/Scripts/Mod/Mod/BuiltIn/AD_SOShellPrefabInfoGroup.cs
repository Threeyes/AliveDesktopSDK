using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// PS：
/// -使用自定义的类，方便新增自定义字段
/// -明确限制类，避免混淆
/// </summary>
[CreateAssetMenu(menuName = AD_EditorDefinition.AssetMenuPrefix_Root + "PrefabInfo/ShellItemGroup", fileName = "ShellItemGroup", order = 1)]
public class AD_SOShellPrefabInfoGroup : AD_SOPrefabInfoGroupBase<AD_SOShellPrefabInfo>
{
    [ContextMenu("InitUsingCurFolder")]
    public void InitUsingCurFolder()
    {
        InitUsingCurFolderFunc();
    }
}
