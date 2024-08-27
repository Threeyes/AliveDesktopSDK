using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// layerNumber：序号，从0开始
/// LayerMask：2的序号次方
/// </summary>
public static class LayerMaskTool
{
    public static int GetLayerNumber(LayerMask layerMask)
    {
        return (int)Mathf.Log(layerMask.value, 2);
    }
    public static int GetLayerMask( int layerNumber)
    {
        return  1<< layerNumber;   // means take 1 and rotate it left by "layer" bit positions
    }

    public static bool IsGameObjectInMask(GameObject a, LayerMask m)
    {
        return ((1 << a.layer) & m) != 0;
    }
}
