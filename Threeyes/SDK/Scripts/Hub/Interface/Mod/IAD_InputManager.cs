using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// </summary>
public interface IAD_InputManager
{ 
    public Vector2 LeftController2DAxis { get; }
    public Vector2 RightController2DAxis { get; }

    public bool IsSpeedUpButtonPressed { get; }
    public bool IsJumpButtonPressed { get; }
}