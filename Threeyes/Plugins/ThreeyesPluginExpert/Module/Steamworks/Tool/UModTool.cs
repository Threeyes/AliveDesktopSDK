using System;
using System.Collections;
using System.Collections.Generic;
using UMod.Shared.Linker;
using UnityEngine;
/// <summary>
/// 
/// Warning：
/// -仅能包含SDK可访问的UMod代码
/// </summary>
public static class UModTool
{
  public static bool  IsUModGameObject(Component component)
    {
        if (!component)
            return false;

        return component.GetComponent<LinkBehaviourV2>() != null;
    }

    /// <summary>
    /// 修复：
    /// -UMod还原继承ISerializationCallbackReceiver的类实例数据后没有调用OnAfterDeserialize接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static T FixSerializationCallbackReceiverData<T>(T data)
        where T : ISerializationCallbackReceiver
    {
        data.OnAfterDeserialize();
        return data;
    }
}
