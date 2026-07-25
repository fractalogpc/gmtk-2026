using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraFramerate : MonoBehaviour
{
    [SerializeField] private int targetFrameRate = 60;
    
    private Camera cam;
    [SerializeField] private RenderTexture renderTexture;

    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.enabled = false;
        renderTexture.Create();
    }

    private float timer = 0f;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f / targetFrameRate)
        {
            timer = 0f;
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = renderTexture };
        }
    }
}
