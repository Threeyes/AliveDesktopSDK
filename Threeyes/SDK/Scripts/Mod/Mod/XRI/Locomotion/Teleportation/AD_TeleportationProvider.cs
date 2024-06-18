using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class AD_TeleportationProvider : TeleportationProvider
{
    /// <summary>
    /// Whether the current teleportation request is valid.
    /// </summary>
    public bool ValidRequest { get { return validRequest; } }
}
