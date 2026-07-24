using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    class TargetValues
    {
        public float azimuth;
        public float elevation;
        public float tolerance;

        public TargetValues(float azimuth, float elevation, float tolerance = 1f)
        {
            this.azimuth = azimuth;
            this.elevation = elevation;
            this.tolerance = tolerance;
        }
    }

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    [SerializeField] private Queue<TargetValues> targetValuesQueue = new Queue<TargetValues>();

    
}
