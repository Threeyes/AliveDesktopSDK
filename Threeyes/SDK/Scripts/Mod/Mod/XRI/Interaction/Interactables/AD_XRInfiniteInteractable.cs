using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// This component makes sure that the attached <c>Interactor</c> always have an interactable selected.
/// This is accomplished by forcing the <c>Interactor</c> to select a new <c>Interactable Prefab</c> instance whenever
/// it loses the current selected interactable.
/// 
/// Ref: UnityEngine.XR.Content.Interaction.XRInfiniteInteractable
/// 
/// Todo:
/// -测试UMod打包后有无问题
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XRBaseInteractor))]
public class AD_XRInfiniteInteractable : MonoBehaviour
{
    /// <summary>
    /// Whether infinite spawning is enabled.
    /// </summary>
    public bool IsActive
    {
        get => m_Active;

        set
        {
            m_Active = value;
            if (enabled && value && !m_Interactor.hasSelection)
                InstantiateAndSelectInteractable();
        }
    }

    [SerializeField]
    [Tooltip("Whether infinite spawning is active.")]
    bool m_Active = true;

    [SerializeField]
    [Tooltip("If true then during Awake the Interactor \"Starting Selected Interactable\" will be overriden by an " +
             "instance of the \"Interactable Prefab\".")]
    bool m_OverrideStartingSelectedInteractable;

    [SerializeField]
    [Tooltip("The Prefab or GameObject to be instantiated and selected.")]
    XRBaseInteractable m_InteractablePrefab;

    XRBaseInteractor m_Interactor;

    void Awake()
    {
        m_Interactor = GetComponent<XRBaseInteractor>();

        if (m_OverrideStartingSelectedInteractable)
            OverrideStartingSelectedInteractable();
    }

    void OnEnable()
    {
        if (m_InteractablePrefab == null)
        {
            Debug.LogWarning("No interactable prefab set - nothing to spawn!");
            enabled = false;
            return;
        }
        m_Interactor.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        m_Interactor.selectExited.RemoveListener(OnSelectExited);
    }

    void OnSelectExited(SelectExitEventArgs selectExitEventArgs)
    {
        if (selectExitEventArgs.isCanceled || !IsActive)
            return;

        InstantiateAndSelectInteractable();
    }

    XRBaseInteractable InstantiateInteractable()
    {
        var socketTransform = m_Interactor.transform;
        return Instantiate(m_InteractablePrefab, socketTransform.position, socketTransform.rotation);
    }

    void OverrideStartingSelectedInteractable()
    {
        m_Interactor.startingSelectedInteractable = InstantiateInteractable();
    }

    void InstantiateAndSelectInteractable()
    {
        if (!gameObject.activeInHierarchy || m_Interactor.interactionManager == null)
            return;

        m_Interactor.interactionManager.SelectEnter((IXRSelectInteractor)m_Interactor, InstantiateInteractable());
    }
}