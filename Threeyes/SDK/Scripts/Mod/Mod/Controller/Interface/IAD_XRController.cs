using Threeyes.GameFramework;

public interface IAD_XRController : IModControllerHandler
{
    /// <summary>
    /// Reset XR Rig to default position
    /// </summary>
    void ResetRigPose();

    /// <summary>
    /// Update Locomotion setting using config info
    /// </summary>
    void UpdateLocomotionSetting();
}
