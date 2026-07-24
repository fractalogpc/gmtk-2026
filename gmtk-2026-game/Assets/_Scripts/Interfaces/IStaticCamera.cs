using Unity.Cinemachine;

public interface IStaticCamera
{
    CinemachineCamera StaticCamera { get; }
    void OnEnterView();
    void OnExitView();
}
