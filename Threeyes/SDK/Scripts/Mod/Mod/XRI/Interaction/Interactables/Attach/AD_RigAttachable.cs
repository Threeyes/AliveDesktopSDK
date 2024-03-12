using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// Mark a Collider/Rigidbody that can be attached by XR Rig （作为辅助标记组件，通过右键调用）
/// 
/// Use case:
/// - Driving/Riding
/// 
/// Warning：
/// -该组件不能继承 TeleportationAnchor 并放在XRBaseInteractable下，因为TeleportationAnchor也是XRBaseInteractable，而多个XRBaseInteractable不能为父子，因为会抢夺碰撞体）
/// </summary>
public class AD_RigAttachable : MonoBehaviour
    , IContextMenuTrigger//由XRManager处理右键菜单信息，因为涉及多语言
{
    public string attachName { get { return m_AttachName; } }//【可空】附着物的名称
    [SerializeField]
    [Tooltip("[Nullable] The detail of the place to attached.")]
    string m_AttachName;

    /// <summary>
    /// The attachment point Unity uses to attach the XR Rig (will use this object if none set).
    /// </summary>
    public Transform attachTransform { get { return m_AttachTransform ? m_AttachTransform : transform; } }
    [SerializeField]
    [Tooltip("The Transform for XR Rig to attached.")]
    Transform m_AttachTransform;//（PS：名称可以统一为【RigAttach Anchor】）

    public AD_XRDestinationRigPart destinationRigPart = AD_XRDestinationRigPart.Foot;//确定attachTransform代表的XR Rig部位


    [ContextMenu("AttachToThis")]
    public void AttachToThis()
    {
        AD_ManagerHolder.XRManager.TeleportAndAttachTo(this);
    }
}