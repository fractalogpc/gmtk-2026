using UnityEngine;

public class DestroyOnDelay : MonoBehaviour
{
    public float delay = 1f;

    private void Start()
    {
        Destroy(gameObject, delay);
    }
}
