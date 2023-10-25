using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// PS:
/// -使用int及string而不是枚举作为参数，可以兼容旧InputSystem，且更加通用
/// </summary>
public interface IAD_InputManager
{
    //public PlayerInput MainPlayerInput { get; }//ToDelete：已经改用OpenXR
}