using System.Collections.Generic;
using UnityEngine;
using Threeyes.Core;

[CreateAssetMenu(menuName = AD_EditorDefinition.AssetMenuPrefix_Root + "PrefabInfo/DecorationItemGroup", fileName = "DecorationItemGroup", order = 101)]
public class AD_SODecorationPrefabInfoGroup : AD_SOPrefabInfoGroupBase<AD_SODecorationPrefabInfo>
{

    [ContextMenu("InitUsingCurFolder")]
    public void InitUsingCurFolder()
    {
        InitUsingCurFolderFunc();
    }

    public static void CreateUsingSelectedObjects()
    {
        CreateUsingSelectedObjectsFunc<AD_SODecorationPrefabInfoGroup>("DecorationItemGroup");
    }
}
