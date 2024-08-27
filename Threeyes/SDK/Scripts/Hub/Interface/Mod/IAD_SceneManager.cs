using Threeyes.GameFramework;

public interface IAD_SceneManager : IHubSceneManager
{
    AD_DecorationPrefabConfigInfo DecorationPrefabConfigInfo { get; }
    AD_ShellPrefabConfigInfo ShellPrefabConfigInfo { get; }
}
