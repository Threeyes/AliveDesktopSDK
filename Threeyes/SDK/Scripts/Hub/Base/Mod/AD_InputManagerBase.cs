using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threeyes.Core;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class AD_InputManagerBase<T> : HubManagerBase<T>, IAD_InputManager
    where T: AD_InputManagerBase<T>
{
    const string action_Move = "Move";
    const string action_Look = "Look";
    const string action_PrimaryButton = "Primary";
    const string action_SecondaryButton = "Secondary";
    const string action_SpeedUp = "Speed Up";
    const string action_Jump = "Jump";

    #region Property

    #region Interface （跨平台的输入）
    public Vector2 LeftController2DAxis { get { return dynamicMoveProvider.LeftHandMoveInput; } }
    public Vector2 RightController2DAxis { get { return dynamicMoveProvider.RightHandMoveInput; } }
    public bool SpeedUpButtonPressed { get { return InputAction_SpeedUp.IsPressed(); } }
    public bool JumpButtonPressed { get { return InputAction_Jump.IsPressed(); } }

    #endregion

    public PlayerInput MainPlayerInput { get { return playerInput; } }
    [SerializeField] PlayerInput playerInput;//通过PlayerInput，将代码中通过GetAxis等调用的方法获取到正确的值
    [SerializeField] AD_DynamicMoveProvider dynamicMoveProvider;//通过该组件直接获取到对应的Action值，不管其是否激活，Action都一直为可用状态

    InputAction InputAction_Move { get { if (inputAction_Move == null) inputAction_Move = playerInput.actions[action_Move]; return inputAction_Move; } }
    InputAction inputAction_Move;
    InputAction InputAction_Look { get { if (inputAction_Look == null) inputAction_Look = playerInput.actions[action_Look]; return inputAction_Look; } }
    InputAction inputAction_Look;
    InputAction InputAction_Primary { get { if (inputAction_Primary == null) inputAction_Primary = playerInput.actions[action_PrimaryButton]; return inputAction_Primary; } }
    InputAction inputAction_Primary;
    InputAction InputAction_Secondary { get { if (inputAction_Secondary == null) inputAction_Secondary = playerInput.actions[action_SecondaryButton]; return inputAction_Secondary; } }
    InputAction inputAction_Secondary;
    InputAction InputAction_SpeedUp { get { if (inputAction_SpeedUp == null) inputAction_SpeedUp = playerInput.actions[action_SpeedUp]; return inputAction_SpeedUp; } }
    InputAction inputAction_SpeedUp;
    InputAction InputAction_Jump { get { if (inputAction_Jump == null) inputAction_Jump = playerInput.actions[action_Jump]; return inputAction_Jump; } }
    InputAction inputAction_Jump;

    //Runtime
    public AD_InputDeviceType curInputDeviceType = AD_InputDeviceType.Keyboard_Mouse;
    #endregion

    #region Unity Method（针对 InputTool 的重载）
    private void Awake()
    {
        //重载Input的实现，改为通过playerInput返回（ToDelete，改为XR实现）
        InputTool.OverrideGetAxis = GetAxis;
        InputTool.OverrideGetMousePosition = GetMousePosition;
        InputTool.OverrideGetMouseButtonDown = GetMouseButtonDown;
        InputTool.OverrideGetMouseButton = GetMouseButton;
        InputTool.OverrideGetMouseButtonUp = GetMouseButtonUp;

        playerInput.onControlsChanged += PlayerInput_OnControlsChanged;
    }
    private void Start()
    {
        PlayerInput_OnControlsChanged(playerInput);//初始化时主动调用一次
    }
    private void OnDestroy()
    {
        InputTool.OverrideGetAxis = null;
        InputTool.OverrideGetMousePosition = null;
        InputTool.OverrideGetMouseButtonDown = null;
        InputTool.OverrideGetMouseButton = null;
        InputTool.OverrideGetMouseButtonUp = null;

        playerInput.onControlsChanged -= PlayerInput_OnControlsChanged;
    }

    /// <summary>
    /// 当不同的设备输入会后被调用，适用于运行时切换
    /// </summary>
    /// <param name="pI"></param>
    public void PlayerInput_OnControlsChanged(PlayerInput pI)
    {
        var firstDevice = pI.devices.FirstOrDefault();
        if (firstDevice != null)
        {
            if (firstDevice is Keyboard)//Keyboard或Mouse都会触发
            {
                curInputDeviceType = AD_InputDeviceType.Keyboard_Mouse;
            }
            else if (firstDevice is Gamepad)
            {
                curInputDeviceType = AD_InputDeviceType.Gamepad;
            }
            else if (firstDevice is XRController) // PS:当前为XR模式时，按下Keyboard/Gamepad也会有反应，但是XRController会频繁更新。接受代码可以通过AD_XRManager.ActivePlatformMode来判断是否要忽略其他Controller
            {
                curInputDeviceType = AD_InputDeviceType.XRController;
            }
        }
        Debug.Log("PlayerInput_OnControlsChanged: " + firstDevice);
        AD_ManagerHolderManager.FirePlayerInputControlChangedEvent(playerInput, curInputDeviceType);
    }
    #endregion

    #region InputTool override
    public float GetAxis(string axisName)
    {
        float result = 0;
        float mosueSensitivity = 0.05f;//参考旧版InputManager，需要针对像素值进行缩放(Ref:https://forum.unity.com/threads/mouse-jittering-with-new-inputsystem.1173761/#post-7526288)
        if (axisName == "Mouse X")
            result = InputAction_Look.ReadValue<Vector2>().x * mosueSensitivity;
        else if (axisName == "Mouse Y")
            result = InputAction_Look.ReadValue<Vector2>().y * mosueSensitivity;
        else if (axisName == "Horizontal")
            result = InputAction_Move.ReadValue<Vector2>().x * mosueSensitivity;
        else if (axisName == "Vertical")
            result = InputAction_Move.ReadValue<Vector2>().y * mosueSensitivity;
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
    public bool GetMouseButtonDown(int index)
    {
        InputAction inputAction = GetInputAction_ButtonFunc(index);
        if (inputAction != null)
            return inputAction.WasPressedThisFrame();
        return false;
    }
    public bool GetMouseButton(int index)
    {
        InputAction inputAction = GetInputAction_ButtonFunc(index);
        if (inputAction != null)
            return inputAction.IsPressed();
        return false;
    }
    public bool GetMouseButtonUp(int index)
    {
        InputAction inputAction = GetInputAction_ButtonFunc(index);
        if (inputAction != null)
            return inputAction.WasReleasedThisFrame();
        return false;
    }

    public Vector2 GetMousePosition()
    {
        //【ToUpdate】：如果是GamePad/XRController，则返回UIGamepadMouseCursor（相对于相机）的位置
        return Mouse.current != null ? Mouse.current.position.ReadValue() : default(UnityEngine.Vector2);

    }
    #endregion

    #region Utility
    InputAction GetInputAction_ValueFunc(string actionName)
    {
        switch (actionName)
        {
            case action_Move:
                return InputAction_Move;
            case action_Look:
                return InputAction_Look;
            default:
                Debug.LogError($"actionname [{actionName}] Not Define!");
                return null;
        }
    }
    InputAction GetInputAction_ButtonFunc(int index)
    {
        //0对应Fire，1对应Menu
        if (index == 0)
            return InputAction_Primary;
        else if (index == 1)
            return InputAction_Secondary;
        else
        {
            Debug.LogError($"Index [{index}] Not Define!");
            return null;
        }
    }
    #endregion
}
