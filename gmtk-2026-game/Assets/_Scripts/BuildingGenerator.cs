using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{

    [SerializeField] private Transform center;
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private int minBuildingCount;
    [SerializeField] private int maxBuildingCount;
    [SerializeField] private float minBuildingSize;
    [SerializeField] private float maxBuildingSize;
    [SerializeField] private float buildingRadius;
    
    private void Start()
    {
        GenerateBuildings();
    }

    public void GenerateBuildings()
    {
        for (int i = center.childCount - 1; i >= 0; i--)
        {
            Destroy(center.GetChild(i).gameObject);
        }

        int buildingCount = Random.Range(minBuildingCount, maxBuildingCount + 1);
        for (int i = 0; i < buildingCount; i++)
        {
            Vector3 position = center.position + Random.insideUnitSphere * buildingRadius;
            position.y = 0;
            float size = Random.Range(minBuildingSize, maxBuildingSize);
            Instantiate(buildingPrefab, position, Quaternion.identity, center).transform.localScale = Vector3.one * size;
        }
    }
}
