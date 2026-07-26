using UnityEngine;

public class TimeLImitLight : MonoBehaviour
{
    [SerializeField] private Light light;
    [SerializeField] private Renderer renderer;
    [SerializeField] private Material materialOn;
    [SerializeField] private Material materialOff;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private Transform rotator;
    [SerializeField] private Vector3 rotationAxis;

    public void Activate()
    {
        light.enabled = true;
        renderer.material = materialOn;
    }

    public void Deactivate()
    {
        light.enabled = false;
        renderer.material = materialOff;
    }

    private void Update()
    {
        rotator.localRotation *= Quaternion.Euler(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}
