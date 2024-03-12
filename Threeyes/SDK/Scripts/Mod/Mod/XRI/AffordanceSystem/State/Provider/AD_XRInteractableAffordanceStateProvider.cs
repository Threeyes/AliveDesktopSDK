using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.State;
/// <summary>
/// Todo：
/// -通过Flag枚举，决定有效的Interactor类型
/// </summary>
public class AD_XRInteractableAffordanceStateProvider : XRInteractableAffordanceStateProvider
{
    public InteractorType interactorType = InteractorType.ControllerInteractor;//目标Interactor（默认排除Socket，避免附着后仍然发生变换）

    protected override void OnFirstHoverEntered(HoverEnterEventArgs args)
    {
        if (!IsTargetInteractor(args))
            return;
        base.OnFirstHoverEntered(args);
    }
    protected override void OnLastHoverExited(HoverExitEventArgs args)
    {
        if (!IsTargetInteractor(args))
            return;
        base.OnLastHoverExited(args);
    }
    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (!IsTargetInteractor(args))
            return;
        base.OnHoverEntered(args);
    }
    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        if (!IsTargetInteractor(args))
            return;
        base.OnHoverExited(args);
    }

    protected override void OnFirstSelectEntered(SelectEnterEventArgs args)
    {
        if (!IsTargetInteractor(args))
            return;
        base.OnFirstSelectEntered(args);
    }
    protected override void OnLastSelectExited(SelectExitEventArgs args)
    {
        if (!IsTargetInteractor(args))
            return;
        base.OnLastSelectExited(args);
    }

    protected override void OnFirstFocusEntered(FocusEnterEventArgs args)
    {
        if (!IsTargetInteractor(args))
            return;
        base.OnFirstFocusEntered(args);
    }
    protected override void OnLastFocusExited(FocusExitEventArgs args)
    {
        if (!IsTargetInteractor(args))
            return;
        base.OnLastFocusExited(args);
    }

    protected override void OnActivatedEvent(ActivateEventArgs args)
    {
        if (!IsTargetInteractor(args))
            return;
        base.OnActivatedEvent(args);
    }
    protected override void OnDeactivatedEvent(DeactivateEventArgs args)
    {
        if (!IsTargetInteractor(args))
            return;
        base.OnDeactivatedEvent(args);
    }

    protected virtual bool IsTargetInteractor(BaseInteractionEventArgs args)
    {
        switch (interactorType)
        {
            case InteractorType.None:
                return false;
            case InteractorType.All:
                return true;
            case InteractorType.ControllerInteractor:
                return args.interactorObject is XRBaseControllerInteractor;
            case InteractorType.Socket:
                return args.interactorObject is XRSocketInteractor;
            default:
                Debug.LogError($"{interactorType} Not Define!");
                return false;
        }
    }
    #region Define
    /// <summary>
    /// Interactor的类型
    /// 
    /// ToUpdate：弄成通用
    /// </summary>
    [System.Flags]
    public enum InteractorType
    {
        None = 0,

        ControllerInteractor = 1 << 0,// Poke/Ray/Direct Interactor （交互相关的）
        Socket = 1 << 1,

        All = ~0
    }
    #endregion
}
