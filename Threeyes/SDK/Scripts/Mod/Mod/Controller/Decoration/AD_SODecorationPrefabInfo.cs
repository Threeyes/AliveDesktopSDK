using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = AD_EditorDefinition.AssetMenuPrefix_Root + "PrefabInfo/DecorationItem", fileName = "DecorationItem",order =100)]
public class AD_SODecorationPrefabInfo : AD_SOPrefabInfo
{
    ///ToAdd:
    ///-增加来源枚举（内置、Workshop或本地外部文件），并针对外部来源增加对应的字段来存储对应路径
    ///-如果读取外部资源时没有预览图（如FBX），就实时生成一张
    ///
    ///Tooltip：
    ///-在原基础上，增加动态生成的Tooltip（也可以增加一个新的Tags字段，用来存储Modder自定义的信息），如：
    ///     -如果Prefab含有Rigidbody且kinematic为false，则增加标识：受重力影响
    ///     -如果Prefab含有BuoyantObject组件，则增加标识：能在水面漂浮
}
