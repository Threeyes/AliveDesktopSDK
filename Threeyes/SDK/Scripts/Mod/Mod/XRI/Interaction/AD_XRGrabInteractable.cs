using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// 功能：
/// -所有AD的可交互组件应使用或参考该组件的实现
/// -保证抓取时不会更改物体的层级，常用于FileSystem/DecorationItem等需要进行层级序列化的物体
/// 
/// Todo：
/// -序列化时保存附着的Socket信息，并在反序列化后重现（或者改为由装饰品的Socket相关保存）
/// </summary>
public class AD_XRGrabInteractable : XRGrabInteractable
{
    private void OnValidate()
    {
        /// PS：
        /// -要取消勾选m_RetainTransformParent，否则从Socket取出时该物体会意外作为Socket的子物体
        if (retainTransformParent)
        {
            retainTransformParent = false;
        }
    }
    protected override void Grab()
    {
        //#1 缓存层级信息
        Transform cacheSceneParent = transform.parent;
        int silbingIndex = transform.GetSiblingIndex();

        //#2 抓取（会导致parent为null）
        base.Grab();//因为父方法有很多无法调用的方法，因此只能先调用

        //#3 还原层级信息
        if (cacheSceneParent)
        {
            transform.SetParent(cacheSceneParent);//【修改】恢复原父子层级，避免Item层级改变导致序列化失败
            transform.SetSiblingIndex(silbingIndex);
        }
    }
}
