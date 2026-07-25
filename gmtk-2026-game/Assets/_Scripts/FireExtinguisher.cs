using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FireExtinguisher : MonoBehaviour
{
    public GameObject obj;

    bool isEnabled;

    private readonly HashSet<E_FireComponent> currentlyExtinguishing = new HashSet<E_FireComponent>();
    private readonly HashSet<E_FireComponent> hitThisFrame = new HashSet<E_FireComponent>();

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.started) Enable();
        else if (context.canceled) Disable();
    }

    private void Enable()
    {
        isEnabled = true;
        obj.SetActive(true);
    }

    private void Disable()
    {
        isEnabled = false;
        obj.SetActive(false);
        ClearExtinguishing();
    }

    private void OnDisable()
    {
        ClearExtinguishing();
    }

    private void Update()
    {
        if (!isEnabled) return;

        hitThisFrame.Clear();

        Vector3 boxCenter = transform.position + transform.forward * 2f;
        Vector3 boxHalfExtents = new Vector3(1f, 1f, 2f);
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, boxHalfExtents, transform.rotation);
        foreach (var hitCollider in hitColliders)
        {
            E_FireComponent fireComponent = hitCollider.GetComponent<E_FireComponent>();
            if (fireComponent != null) hitThisFrame.Add(fireComponent);
        }

        foreach (var fc in currentlyExtinguishing)
        {
            if (fc != null && !hitThisFrame.Contains(fc)) fc.BeingExtinguished = false;
        }
        foreach (var fc in hitThisFrame) fc.BeingExtinguished = true;

        currentlyExtinguishing.Clear();
        foreach (var fc in hitThisFrame) currentlyExtinguishing.Add(fc);
    }

    private void ClearExtinguishing()
    {
        foreach (var fc in currentlyExtinguishing)
        {
            if (fc != null) fc.BeingExtinguished = false;
        }
        currentlyExtinguishing.Clear();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 boxHalfExtents = new Vector3(1f, 1f, 2f);
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + transform.forward * 2f, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2);
        Gizmos.matrix = prev;
    }
}
