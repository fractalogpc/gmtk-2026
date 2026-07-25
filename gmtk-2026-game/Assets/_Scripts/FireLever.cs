using UnityEngine;

public class FireLever : MonoBehaviour
{
    [SerializeField] private DragLever lever;
    [SerializeField][Range(0, 1)] private float fireThreshold = 0.8f; 
    public bool IsFired => hasFired;
    private bool hasFired = false;

    public void ResetFireState(float speed = -1f)
    {
        hasFired = false;
        lever.ResetLever(speed);
    }
    
    private void Update()
    {
        if (!hasFired && lever.NormalizedValue >= fireThreshold)
        {
            hasFired = true;
            Fire();
        }
        else if (hasFired && lever.NormalizedValue < fireThreshold)
        {
            hasFired = false; // Reset the fire state when the lever is released
        }
    }

    private void Fire()
    {
        // play sounds or something
    }
}
