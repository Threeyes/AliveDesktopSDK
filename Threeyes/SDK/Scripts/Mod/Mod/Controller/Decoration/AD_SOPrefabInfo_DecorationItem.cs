using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = AD_EditorDefinition.AssetMenuPrefix_Root + "PrefabInfo/DecorationItem", fileName = "DecorationItem")]
public class AD_SOPrefabInfo_DecorationItem : AD_SOPrefabInfoBase
{
    ///ToAdd:
    ///-增加来源枚举（内置、Workshop或本地外部文件），并针对外部来源增加对应的字段来存储对应路径
    ///-如果读取外部资源时没有预览图（如FBX），就实时生成一张
}
