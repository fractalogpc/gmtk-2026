using UnityEngine;

public class FireLever : MonoBehaviour
{
    [SerializeField] private Lever lever;
    [SerializeField][Range(0, 1)] private float fireThreshold = 0.8f; 
    public bool IsFired => hasFired;
    private bool hasFired = false;

    public void ResetFireState()
    {
        hasFired = false;
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
