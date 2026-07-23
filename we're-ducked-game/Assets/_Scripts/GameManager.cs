using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool hasSavedPlayerState;
    public Vector3 savedPlayerPosition;
    public Quaternion savedPlayerRotation;

    public string mainSceneName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject(nameof(GameManager));
        go.AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SavePlayerState(Transform player)
    {
        savedPlayerPosition = player.position;
        savedPlayerRotation = player.rotation;
        hasSavedPlayerState = true;
    }

    public void ApplyPlayerState(Transform player)
    {
        if (!hasSavedPlayerState) return;
        player.position = savedPlayerPosition;
        player.rotation = savedPlayerRotation;
    }
}
