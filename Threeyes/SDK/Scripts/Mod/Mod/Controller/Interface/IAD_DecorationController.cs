using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAD_DecorationController : IAD_SerializableItemController<AD_DecorationItemInfo>
{
    /// <summary>
    /// Add new decoration item
    /// </summary>
    /// <param name="sOPrefabInfoBase"></param>
    /// <param name="initPosition"></param>
    GameObject CreateElement(GameObject prefab, Vector3? initPosition, Quaternion? initRotation = null, Action<GameObject> actionCreateCompleted = null);

    /// <summary>
    /// Delete exist decoration item
    /// </summary>
    /// <param name="item"></param>
    void DeleteElement(IAD_DecorationItem item);
}