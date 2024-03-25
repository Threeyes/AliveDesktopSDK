using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAD_DecorationController : IAD_SerializableItemController<AD_DecorationItemInfo>
{
    /// <summary>
    /// 添加新装饰物
    /// </summary>
    /// <param name="sOPrefabInfoBase"></param>
    /// <param name="initPosition"></param>
    void AddElement(GameObject prefab, Vector3? initPosition, Quaternion? initRotation = null) { }
}