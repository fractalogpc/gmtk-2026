using UnityEngine;
using UnityEngine.Events;

public class SuccessLight : MonoBehaviour
{
    public UnityEvent OnSuccess;
    public UnityEvent OnFailure;
    public UnityEvent OnReset;

    public void Reset()
    {
        OnReset?.Invoke();
    }

    public void SetSuccess(bool isSuccess)
    {
        if (isSuccess)
        {
            OnSuccess?.Invoke();
        }
        else
        {
            OnFailure?.Invoke();
        }
    }
}
