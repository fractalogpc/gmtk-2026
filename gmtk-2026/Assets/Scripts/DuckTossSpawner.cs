using UnityEngine;

public class DuckTossSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject duckPrefab;

    [SerializeField]
    private GameObject duck;

    public void SpawnDuck(Vector3 position, float speed)
    {
        duck = GameObject.Instantiate(duckPrefab, position, Quaternion.identity);
        DuckTossDuck duckScript = duck.GetComponent<DuckTossDuck>();
        duckScript.speed = speed;
    }

    void Update()
    {
        if (duck == null)
        {
            SpawnDuck(new Vector3(-10,-1.75f,0), Random.Range(0.5f, 1.5f));
        }
    }

}
