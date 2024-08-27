using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threeyes.InputSystem;
using Threeyes.GameFramework;
using Threeyes.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using System;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.UI;
/// <summary>
/// Warning:
/// -即使是XR模拟器，也会进入XR的scheme，因此需要删掉
/// -ControlScheme中的设备需要列全（即使是Optional也要列出），否则会造成无法触发（如Gamepad的VirtualMouse）
/// 
/// Todo：
/// -VR左右手柄的键分别对应Gamepad的XY和AB，暂时交互与Gamepad一致
/// 
/// Bug:
/// -安卓：通过侧边滑动触发返回时，会模拟按键，导致切换到键鼠模式
/// </summary>
/// <typeparam name="T"></typeparam>
public class AD_InputManagerBase<T> : HubManagerBase<T>, IAD_InputManager
    where T : AD_InputManagerBase<T>
{
    #region Interface （跨平台的输入）
    public Vector2 LeftController2DAxis { get { return dynamicMoveProvider.LeftHandMoveInput; } }
    public Vector2 RightController2DAxis { get { return dynamicMoveProvider.RightHandMoveInput; } }
    public bool IsSpeedUpButtonPressed { get { return ActiveGroup.inputAction_SpeedUp.IsPressed(); } }//用于车辆加速等。对应XR/Gamepad的右摇杆按下
    public bool IsJumpButtonPressed { get { return ActiveGroup.inputAction_Jump.IsPressed(); } }//用于跳跃或刹车。对应XR的左手柄X，或Gamepad的West按键
    #endregion

    #region Property
    public bool IsCurKeyboardMouseMode { get { return curInputDeviceType == AD_InputDeviceType.Keyboard_Mouse; } }
    public bool IsCurXRMode { get { return curInputDeviceType == AD_InputDeviceType.XRController; } }
    public bool IsCurTouchScreenMode { get { return curInputDeviceType == AD_InputDeviceType.Gamepad_TouchScreen; } }
    public bool IsCurGamepadMode { get { return IsGamepadMode(curInputDeviceType); } }/// Gamepad or Gamepad_TouchScreen
    public bool IsGamepadMode(AD_InputDeviceType deviceType)
    {
        return deviceType == AD_InputDeviceType.Gamepad || deviceType == AD_InputDeviceType.Gamepad_TouchScreen;
    }


    public PlayerInput MainPlayerInput { get { return playerInput; } }
    [SerializeField] PlayerInput playerInput;//通过PlayerInput，将代码中通过GetAxis等调用的方法获取到正确的值
    [SerializeField] AD_DynamicMoveProvider dynamicMoveProvider;//通过该组件直接获取到对应的Action值，不管其是否激活，Action都一直为可用状态（ToUpdate：改为直接通过Action获取）
    [SerializeField] EventSystem eventSystem;
    [SerializeField] XRUIInputModule xRUIInputModule;

    InputActionGroup ActiveGroup
    {
        get
        {
            switch (curInputDeviceType)
            {
                case AD_InputDeviceType.XRController:
                    return inputActionCollection_XR;
                default:
                    return inputActionCollection_Common;
            }
        }
    }
    [SerializeField] InputActionGroup inputActionCollection_Common;
    [SerializeField] InputActionGroup inputActionCollection_XR;

    //Runtime
    public AD_InputDeviceType curInputDeviceType = AD_InputDeviceType.Null;
    public static bool IsEditorSimulatedTouchscreenMode;
    #endregion

    #region Unity Method（针对 InputTool 的重载）

    protected virtual void Awake()
    {
        //重载Input的实现，改为通过playerInput返回（ToDelete，改为XR实现）
        InputTool.OverrideGetAxis = GetAxis;
        InputTool.OverrideGetMousePosition = GetMousePosition;
        InputTool.OverrideGetMouseButtonDown = GetMouseButtonDown;
        InputTool.OverrideGetMouseButton = GetMouseButton;
        InputTool.OverrideGetMouseButtonUp = GetMouseButtonUp;
        InputTool.OverrideGetMouseWheelDelta = GetMouseWheelDelta;
        InputTool.OverrideGetKeyDown = GetKeyDown;
        InputTool.OverrideGetKey = GetKey;
        InputTool.OverrideGetKeyUp = GetKeyUp;
        UITool.OverrideIsHoveringUI = () => IsHoveringUI;

        //#Init Actions
        inputActionCollection_Common.EnableAction();
        inputActionCollection_XR.EnableAction();

        playerInput.onControlsChanged += OnPlayerInputControlsChanged;
        InputSystem.onActionChange += OnActionChanged;


        xRUIInputModule.finalizeRaycastResults += OnfinalizeRaycastResults;
        //xRUIInputModule.pointerEnter//Fallback:监听进入、退出

        //OnPlayerInputControlsChanged(playerInput);//初始化时主动调用一次(ToUpdate:基于该Scheme，初始化一次curInputDeviceType，后续由OnActionChanged负责更新)
    }

    [Multiline]
    public string debugRaycastResult;
    public Text debugText;

    public Vector2 screenPosition_XR;
    public bool isHoveringUI_XR;//标记是否在UI上，避免Left射线射中UI背面的物体，但不在UI的情况

    /// <summary>
    /// EventSystem的Raycast射中的物体
    /// Ref： UIInputModule#638lazyfollow
    /// </summary>
    /// <param name="data"></param>
    /// <param name="list"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnfinalizeRaycastResults(PointerEventData eventData, List<RaycastResult> list)
    {
        if (curInputDeviceType != AD_InputDeviceType.XRController) return;//ToUse

        ///Warning:
        ///-在调用此回调时，position会被设置为无效值（UIInputModule.ProcessTrackedDevice#639行）
        ///-XR模式下，PointerEventData的position是从Mouse、Pen等设备传入，所以默认是无效值，因此要自行处理（比如计算RaycastResult所在位置）
        ///     -其实就是ProcessTrackedDevice中的eventData.position
        RaycastResult pointerCurrentRaycast = FindFirstRaycast(list);
        Camera screenPointCamera = ManagerHolder.EnvironmentManager.MainCamera;//ToUpdate
        if (pointerCurrentRaycast.isValid)//该目标点有效
        {
            screenPosition_XR = screenPointCamera.WorldToScreenPoint(pointerCurrentRaycast.worldPosition);
            //Debug.LogError("[OnfinalizeRaycastResults]: isValid");
            isHoveringUI_XR = true;
        }
        else//该目标点无效（ToUpdate：应该直接返回LeftRay的对应Ray点）
        {
            var xRRayInteractor = AD_ManagerHolder.XRManager.LeftController.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.XRRayInteractor>();

            if (xRUIInputModule.GetTrackedDeviceModel(xRRayInteractor, out TrackedDeviceModel model))
            {
                List<Vector3> rayPoints = model.raycastPoints;
                var endPosition = rayPoints.Count > 0 ? rayPoints[rayPoints.Count - 1] : Vector3.zero;//Ray最终的点
                screenPosition_XR = screenPointCamera.WorldToScreenPoint(endPosition);
                eventData.position = screenPosition_XR;
            }
            else
            {
                screenPosition_XR = new Vector2(-1, -1);
            }

            isHoveringUI_XR = false;
        }


        if (debugText)
            debugText.text = eventData.ToString();
    }

    protected static RaycastResult FindFirstRaycast(List<RaycastResult> candidates)
    {
        var candidatesCount = candidates.Count;
        for (var i = 0; i < candidatesCount; ++i)
        {
            if (candidates[i].gameObject == null)
                continue;

            return candidates[i];
        }
        return new RaycastResult();
    }
    ///// <summary>
    //    /// 世界坐标转为鼠标坐标（注意：这里返回屏幕坐标系是正确的，因为后续的UI判断也是通过该点投射对应的射线）
    //    /// Ref:https://discussions.unity.com/t/how-to-convert-from-world-space-to-canvas-space/117981/9
    //    /// </summary>
    //    /// <returns></returns>
    //private Vector2 WorldToCanvasPosition(RectTransform canvas, Camera camera, Vector3 position)
    //{
    //    //Vector position (percentage from 0 to 1) considering camera size.
    //    //For example (0,0) is lower left, middle is (0.5,0.5)
    //    Vector2 temp = camera.WorldToViewportPoint(position);

    //    //Calculate position considering our percentage, using our canvas size
    //    //So if canvas size is (1100,500), and percentage is (0.5,0.5), current value will be (550,250)
    //    temp.x *= canvas.sizeDelta.x;
    //    temp.y *= canvas.sizeDelta.y;

    //    //The result is ready, but, this result is correct if canvas recttransform pivot is 0,0 - left lower corner.
    //    //But in reality its middle (0.5,0.5) by default, so we remove the amount considering cavnas rectransform pivot.
    //    //We could multiply with constant 0.5, but we will actually read the value, so if custom rect transform is passed(with custom pivot) , 
    //    //returned value will still be correct.

    //    //temp.x -= canvas.sizeDelta.x * canvas.pivot.x;
    //    //temp.y -= canvas.sizeDelta.y * canvas.pivot.y;

    //    return temp;
    //}


    private void OnDestroy()
    {
        InputTool.OverrideGetAxis = null;
        InputTool.OverrideGetMousePosition = null;
        InputTool.OverrideGetMouseButtonDown = null;
        InputTool.OverrideGetMouseButton = null;
        InputTool.OverrideGetMouseButtonUp = null;
        InputTool.OverrideGetMouseWheelDelta = null;
        InputTool.OverrideGetKeyDown = null;
        InputTool.OverrideGetKey = null;
        InputTool.OverrideGetKeyUp = null;

        playerInput.onControlsChanged -= OnPlayerInputControlsChanged;
    }

    /// <summary>
    /// Event that is signalled when the state of enabled actions in the system changes or
    /// when actions are triggered.
    /// 
    /// Bug:
    /// -编辑模式下，如果激活SimulateTouch，那么还是会获取到持续的KeyboardMouse信息
    /// </summary>
    /// <param name="arg1"></param>
    /// <param name="change"></param>
    private void OnActionChanged(object arg1, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed && arg1 is InputAction inputAction)
        {
            InputDevice inputDevice = inputAction.activeControl.device;
            List<string> listUsage = inputDevice.usages.ToList().ConvertAll(iS => iS.ToString());
            //Debug.LogError("[TempActionChanged]: " + inputDevice.displayName + " " + inputAction.activeControl.path + " " + curInputDeviceType);

            if (inputDevice.displayName == "VirtualMouse") return;//忽略VirtualMouse（Mouse、Gamepad等都会触发，导致意外切换到Keyboard&Mouse的scheme中）
            if (inputDevice is Gamepad && listUsage.Exists(iS => iS.ToString() == "OnScreen")) return;//忽略VirtualGamepad的输入

            if (curInputDeviceType != AD_InputDeviceType.Null) //非初始化时忽略硬件鼠标持续更新的Pos，避免Type频繁切换导致画面闪烁
            {
                if (inputAction.activeControl.path == "/Mouse/position" || inputAction.activeControl.path == "/Mouse/delta" || inputAction.activeControl.path == "/Touchscreen/position")
                    return;
            }
            if (PlatformTool.IsRuntimeAndroidBuild)//安卓端：忽略滑动屏幕侧边模拟的Escape键，导致Action意外切换到键鼠模式
            {
                if (inputDevice.displayName == "Virtual") return;
            }



            //Usages：非必须  (如屏幕的Gamepad为OnScreen）
            string usageInfo = "";
            if (listUsage.Count > 0)
            {
                usageInfo = "Usage: " + listUsage.ConnectToString(",");
            }

            AD_InputDeviceType tempInputDeviceType = AD_InputDeviceType.Null;
            bool shouldIgnoreKeyboardMoue =
#if UNITY_EDITOR
                IsEditorSimulatedTouchscreenMode;//SimulatedTouch模式下忽略键鼠，避免频繁切换
#else
                false;
#endif
            if (!shouldIgnoreKeyboardMoue && (inputDevice is Keyboard || inputDevice is Mouse))
            {
                tempInputDeviceType = AD_InputDeviceType.Keyboard_Mouse;
            }
            else if (inputDevice is Gamepad)
            {
                tempInputDeviceType = AD_InputDeviceType.Gamepad;
            }
            else if (inputDevice is Touchscreen)
            {
                tempInputDeviceType = AD_InputDeviceType.Gamepad_TouchScreen;
            }
            //else if (Touchscreen.current != null)//如果有触摸屏，则Fallback
            //{
            //    tempInputDeviceType = AD_InputDeviceType.Gamepad_TouchScreen;
            //}
            else if (inputDevice is XRController)
            {
                if (!(inputDevice is XRSimulatedController || inputDevice is XRSimulatedHMD))//忽略模拟器
                {
                    tempInputDeviceType = AD_InputDeviceType.XRController;
                }
            }

            if (SetInputDeviceType(tempInputDeviceType))
            {
                //Debug.LogError($"[OnActionChanged]: {inputDevice.displayName} [{inputAction.activeControl.path}]  " + curInputDeviceType);
            }
            //if (curInputDeviceType != tempInputDeviceType && tempInputDeviceType != AD_InputDeviceType.Null)
            //{
            //    curInputDeviceType = tempInputDeviceType;
            //    //playerInput.user.ActivateControlScheme(curInputDeviceType.ToString());//主动切换Scheme(缺点：无法传递对应的Devices，导致无法正常监听,playerInput.SwitchCurrentControlScheme同理)

            //    OnControlsChangedFunc(curInputDeviceType);
            //    AD_ManagerHolderManager.FirePlayerInputControlChangedEvent(playerInput, curInputDeviceType);
            //}
        }
    }

    bool SetInputDeviceType(AD_InputDeviceType tempInputDeviceType)
    {
        if (curInputDeviceType != tempInputDeviceType && tempInputDeviceType != AD_InputDeviceType.Null)
        {
            curInputDeviceType = tempInputDeviceType;
            //playerInput.user.ActivateControlScheme(curInputDeviceType.ToString());//主动切换Scheme(缺点：无法传递对应的Devices，导致无法正常监听,playerInput.SwitchCurrentControlScheme同理)

            OnControlsChangedFunc(curInputDeviceType);
            AD_ManagerHolderManager.FirePlayerInputControlChangedEvent(playerInput, curInputDeviceType);

            return true;
        }
        return false;
    }

    /// <summary>
    /// 当不同的设备输入会后被调用，适用于运行时切换
    /// 
    /// ToUpdate：
    /// -改为通过scheme或上面的OnActionChanged来判断
    /// -针对平台特殊处理（比如检测到安卓端的devices包含Touchscreen，那就不主动切换）
    /// -自行处理对应的Scheme切换
    /// 
    /// Bug:
    /// -有可能顺序不对，导致无法正确识别当前正在使用的设备
    /// </summary>
    /// <param name="pI"></param>
    public void OnPlayerInputControlsChanged(PlayerInput pI)
    {
        //Debug.LogError("[OnPlayerInputControlsChanged] currentControlScheme:" + pI.currentControlScheme + $"Devices: [{pI.devices.ConnectToString(", ")}]");//每次更新后进行提示

        //if(curInputDeviceType== AD_InputDeviceType.Null)
        //{
        //    string currentControlScheme = pI.currentControlScheme;
        //}


        ///Warning: 
        ///-VirtualMouse会导致PlayerInput意外切换到Keyboard&Mouse的scheme中，从而导致Gamepad出异常。【解决办法：通过上面的OnActionChanged来自行切换Scheme，并通过检查是否为VirtualMouse来排除】
        //var firstDevice = pI.devices.FirstOrDefault();
        //if (firstDevice != null)
        //{
        //    if (firstDevice is Keyboard)//Keyboard或Mouse都会触发
        //    {
        //        curInputDeviceType = AD_InputDeviceType.Keyboard_Mouse;
        //    }
        //    else if (firstDevice is Gamepad)
        //    {
        //        curInputDeviceType = AD_InputDeviceType.Gamepad;
        //    }
        //    else if (firstDevice is XRController) // PS:当前为XR模式时，按下Keyboard/Gamepad也会有反应，但是XRController会频繁更新。接受代码可以通过AD_XRManager.ActivePlatformMode来判断是否要忽略其他Controller
        //    {
        //        curInputDeviceType = AD_InputDeviceType.XRController;
        //    }
        //    //Debug.Log($"【Test】PlayerInput_OnControlsChanged [{curInputDeviceType}]: " + pI.devices.ToList().ConnectToString(","));
        //}

        //OnControlsChangedFunc(curInputDeviceType);
        //AD_ManagerHolderManager.FirePlayerInputControlChangedEvent(playerInput, curInputDeviceType);
    }

    /// <summary>
    /// 输入变换回调
    /// </summary>
    /// <param name="inputDeviceType"></param>
    protected virtual void OnControlsChangedFunc(AD_InputDeviceType inputDeviceType)
    {

    }

    #endregion

    #region InputTool override

    public virtual Vector2 GetMousePosition()
    {
        /////【ToUpdate】：
        /////-如果是XR，返回手柄射线与物体的交汇点（参考AD_ObjectPlacerManager），或UI的交汇点（从EventSystem中获取）
        if (curInputDeviceType == AD_InputDeviceType.XRController)
        {
            return screenPosition_XR;
            //return Vector2.zero;
            //UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule xRUIInputModule;
            //xRUIInputModule.pointerEnter
            //return eventSystem.
        }
        return ActiveGroup.inputAction_Position.ReadValue<Vector2>();
        //return Mouse.current != null ? Mouse.current.position.ReadValue() : default(UnityEngine.Vector2);
    }

    /// <summary>
    /// 对应Move/Look
    /// </summary>
    /// <param name="axisName"></param>
    /// <returns></returns>
    public virtual float GetAxis(string axisName)
    {
        if (curInputDeviceType == AD_InputDeviceType.XRController)//XR中无对应的InputAction，且摇杆值由XR自行处理，这里的Axis仅适用于模拟器移动
        {
            return 0;
        }
        float result = 0;
        float mosueSensitivity = 0.05f;//【ToTest】参考旧版InputManager，需要针对像素值进行缩放(Ref:https://forum.unity.com/threads/mouse-jittering-with-new-inputsystem.1173761/#post-7526288)
        if (axisName == "Horizontal")
            result = ActiveGroup.inputAction_Move.ReadValue<Vector2>().x;
        else if (axisName == "Vertical")
            result = ActiveGroup.inputAction_Move.ReadValue<Vector2>().y;
        else if (axisName == "Mouse X")
            result = ActiveGroup.inputAction_Look.ReadValue<Vector2>().x * mosueSensitivity;
        else if (axisName == "Mouse Y")
            result = ActiveGroup.inputAction_Look.ReadValue<Vector2>().y * mosueSensitivity;
        else
        {
            Debug.LogError($"axisName [{axisName}] Not Define!");
        }
        return result;
    }

    ///等效于旧Input的NewInputSystem方法关系如下，Ref:https://forum.unity.com/threads/how-would-you-handle-a-getbuttondown-situaiton-with-the-new-input-system.627184/#post-6462337
    /// GetButton("fire")=playerInput.actions["fire"].IsPressed()
    /// GetButtonDown("fire")=playerInput.actions["fire"].WasPressedThisFrame()
    /// GetButtonUp("fire")=playerInput.actions["fire"].WasReleasedThisFrame()
    public virtual bool GetMouseButtonDown(int index)
    {
        InputAction inputAction = GetInputAction_ButtonFunc(index);
        if (inputAction != null)
        {
            //Debug.LogError("GetMouseButtonDown" + inputAction.WasPressedThisFrame());
            return inputAction.WasPressedThisFrame();//按下那一帧才会触发
        }
        return false;
    }
    public virtual bool GetMouseButton(int index)
    {
        InputAction inputAction = GetInputAction_ButtonFunc(index);
        if (inputAction != null)
        {
            //Debug.LogError("GetMouseButton" + inputAction.WasPressedThisFrame());
            return inputAction.IsPressed();
        }
        return false;
    }
    public virtual bool GetMouseButtonUp(int index)
    {
        InputAction inputAction = GetInputAction_ButtonFunc(index);
        if (inputAction != null)
        {
            //Debug.LogError("GetMouseButtonUp" + inputAction.WasPressedThisFrame());
            return inputAction.WasReleasedThisFrame();//抬起
        }
        return false;
    }

    public virtual Vector2 GetMouseWheelDelta()
    {
        //Debug.LogError("GetMouseWheelDelta:" + InputAction_ScrollWheel.ReadValue<Vector2>());
        return ActiveGroup.inputAction_ScrollWheel.ReadValue<Vector2>();
    }


    bool GetKeyDown(KeyCode oldKey)
    {
        InputAction inputActionTarget = null;
        if (oldKey == KeyCode.Return || oldKey == KeyCode.KeypadEnter)
        {
            inputActionTarget = ActiveGroup.inputAction_Submit;
        }
        if (oldKey == KeyCode.Escape)
        {
            inputActionTarget = ActiveGroup.inputAction_Cancel;
        }
        if (inputActionTarget != null)
            return inputActionTarget.WasPressedThisFrame();

        var buttonControl = InputTool.GetButtonControl(oldKey); return buttonControl != null ? buttonControl.wasPressedThisFrame : false;
    }
    bool GetKey(KeyCode oldKey)
    {
        InputAction inputActionTarget = null;
        if (oldKey == KeyCode.Return || oldKey == KeyCode.KeypadEnter)
        {
            inputActionTarget = ActiveGroup.inputAction_Submit;
        }
        if (oldKey == KeyCode.Escape)
        {
            inputActionTarget = ActiveGroup.inputAction_Cancel;
        }
        if (inputActionTarget != null)
            return inputActionTarget.IsPressed();

        var buttonControl = InputTool.GetButtonControl(oldKey); return buttonControl != null ? buttonControl.isPressed : false;
    }
    bool GetKeyUp(KeyCode oldKey)
    {
        InputAction inputActionTarget = null;
        if (oldKey == KeyCode.Return || oldKey == KeyCode.KeypadEnter)
        {
            inputActionTarget = ActiveGroup.inputAction_Submit;
        }
        if (oldKey == KeyCode.Escape)
        {
            inputActionTarget = ActiveGroup.inputAction_Cancel;
        }
        if (inputActionTarget != null)
            return inputActionTarget.WasReleasedThisFrame();

        var buttonControl = InputTool.GetButtonControl(oldKey); return buttonControl != null ? buttonControl.wasReleasedThisFrame : false;
    }

    protected virtual bool IsHoveringUI
    {
        get
        {
            if (curInputDeviceType == AD_InputDeviceType.XRController)//XR:检测射线是否在UI上
            {
                return isHoveringUI_XR;
            }
            return IsHoveringUI_Internal;
        }
    }
    static EventSystem curEventSystem { get { if (!_curEventSystem) _curEventSystem = EventSystem.current; return _curEventSystem; } }
    static EventSystem _curEventSystem;
    static bool IsHoveringUI_Internal
    {
        get
        {
            if (curEventSystem)
            {
                return curEventSystem.IsPointerOverGameObject();
            }
            return false;
        }
    }
    #endregion

    #region Define

    /// <summary>
    /// 包含了某一设备下的Action组合。部分可空
    /// </summary>
    [Serializable]
    public class InputActionGroup
    {
        [SerializeField] InputActionReference inputActionRef_Position;
        [SerializeField] InputActionReference inputActionRef_Move;
        [SerializeField] InputActionReference inputActionRef_Look;
        [SerializeField] InputActionReference inputActionRef_LeftClick;
        [SerializeField] InputActionReference inputActionRef_RightClick;
        [SerializeField] InputActionReference inputActionRef_ScrollWheel;
        [SerializeField] InputActionReference inputActionRef_SpeedUp;
        [SerializeField] InputActionReference inputActionRef_Jump;
        [SerializeField] InputActionReference inputActionRef_Submit;
        [SerializeField] InputActionReference inputActionRef_Cancel;

        public InputAction inputAction_Position;
        public InputAction inputAction_Move;
        public InputAction inputAction_Look;
        public InputAction inputAction_LeftClick;
        public InputAction inputAction_RightClick;
        public InputAction inputAction_ScrollWheel;
        public InputAction inputAction_Submit;//相当于按键A
        public InputAction inputAction_Cancel;//相当于按键B
        public InputAction inputAction_SpeedUp;//相当于右摇杆按下
        public InputAction inputAction_Jump;//相当于按键X（特殊按键1）

        public void EnableAction()
        {
            inputAction_Position = EnableAction(inputActionRef_Position);
            inputAction_Move = EnableAction(inputActionRef_Move);
            inputAction_Look = EnableAction(inputActionRef_Look);
            inputAction_LeftClick = EnableAction(inputActionRef_LeftClick);
            inputAction_RightClick = EnableAction(inputActionRef_RightClick);
            inputAction_ScrollWheel = EnableAction(inputActionRef_ScrollWheel);
            inputAction_Submit = EnableAction(inputActionRef_Submit);
            inputAction_Cancel = EnableAction(inputActionRef_Cancel);
            inputAction_SpeedUp = EnableAction(inputActionRef_SpeedUp);
            inputAction_Jump = EnableAction(inputActionRef_Jump);
        }

        static InputAction EnableAction(InputActionReference actionReference)
        {
            if (!actionReference) return null;
            InputAction action = actionReference.action;
            if (action != null && !action.enabled)
                action.Enable();
            return action;
        }
    }
    #endregion

    #region Utility

    //InputAction GetinputAction_ValueFunc(string actionName)
    //{
    //    switch (actionName)
    //    {
    //        case action_Move:
    //            return ActiveGroup.inputAction_Move;
    //        case action_Look:
    //            return ActiveGroup.inputAction_Look;
    //        default:
    //            Debug.LogError($"actionname [{actionName}] Not Define!");
    //            return null;
    //    }
    //}
    InputAction GetInputAction_ButtonFunc(int index)
    {
        //0对应Fire，1对应Menu
        if (index == 0)
            return ActiveGroup.inputAction_LeftClick;
        else if (index == 1)
            return ActiveGroup.inputAction_RightClick;
        else//某些Scheme可能没有定义中建对应的按键，不算报错
        {
            //Debug.LogWarning($"Index [{index}] Not Define in thi scheme!");
            return null;
        }
    }
    #endregion
}
