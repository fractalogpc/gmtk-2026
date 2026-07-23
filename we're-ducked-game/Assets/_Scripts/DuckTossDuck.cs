using UnityEngine;

public class DuckTossDuck : MonoBehaviour
{
    [SerializeField]
    public float speed;

    private static float duckHeight;
    private bool movingLeft = false;
    [SerializeField]
    private bool fed = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        duckHeight = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (!fed)
        {
            // Bob in water
            float y = duckHeight + Mathf.Sin(Time.time * 2f) * 0.1f;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);

            if (transform.position.x < -7.5f)
            {
                movingLeft = false;
                transform.localScale = new Vector3(1f, 1f, 1f);
            }
            else if (transform.position.x > 5f)
            {
                movingLeft = true;
                transform.localScale = new Vector3(-1f, 1f, 1f);
            }
            
            transform.position += new Vector3((movingLeft ? -1f : 1f) * Time.deltaTime * speed, 0, 0);
        } else {
            // Fly away
            transform.position += new Vector3(0, 1f * Time.deltaTime * speed, 0);

            if (transform.position.y > 10f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Grape"))
        {
            fed = true;
            Destroy(other.gameObject);
        }
    }
}
