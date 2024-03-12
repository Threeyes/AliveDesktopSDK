using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
/// <summary>
/// 实现：
/// -参考AD_XRInteractableAffordanceStateProvider
/// </summary>
public class AD_XRInteractableEventListener : MonoBehaviour
{
    public virtual XRBaseInteractable Interactable
    {
        get
        {
            if (!m_Interactable)
                m_Interactable = GetCompFunc();
            return m_Interactable;
        }
    }
    [SerializeField] XRBaseInteractable m_Interactable;

    [SerializeField] BoolEvent onActivateDeactivate;
    [SerializeField] UnityEvent onActivate;
    [SerializeField] UnityEvent onDeactivate;
    void OnEnable()
    {
        AddListeners();
    }
    void OnDisable()
    {
        RemoveListeners();
    }

    protected virtual void AddListeners()
    {
        if(!Interactable)
        {
            Debug.LogError($"{nameof(m_Interactable)} is null!");
            return;
        }
        Interactable.activated.AddListener(OnActivated);
        Interactable.deactivated.AddListener(OnDeactivated);
    }
    protected virtual void RemoveListeners()
    {
        if (!Interactable)
        {
            Debug.LogError($"{nameof(m_Interactable)} is null!");
            return;
        }
        Interactable.activated.RemoveListener(OnActivated);
        Interactable.deactivated.RemoveListener(OnDeactivated);
    }

    void OnActivated(ActivateEventArgs args)
    {
        onActivateDeactivate.Invoke(true);
        onActivate.Invoke();
    }
    void OnDeactivated(DeactivateEventArgs args)
    {
        onActivateDeactivate.Invoke(false);
        onDeactivate.Invoke();
    }


    protected virtual XRBaseInteractable GetCompFunc()
    {
        if (this)//避免物体被销毁
            return GetComponent<XRBaseInteractable>();
        return null;
    }
}
