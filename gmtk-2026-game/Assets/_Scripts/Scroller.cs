using UnityEngine;
using System.Collections;

public class Scroller : MonoBehaviour
{

    [SerializeField] private float startYPos;
    [SerializeField] private float endYPos;
    [SerializeField] private float scrollTime;

    [SerializeField] private Transform targetTransform;

    private float timer = 10f;

    public void Scroll()
    {
        timer = 0f;
    }

    private void Update()
    {
        if (targetTransform != null)
        {
            float y = Mathf.Lerp(startYPos, endYPos, Mathf.Clamp01(timer / scrollTime));
            targetTransform.localPosition = new Vector3(targetTransform.localPosition.x, y, targetTransform.localPosition.z);
            timer += Time.deltaTime;
        }
    }

}
