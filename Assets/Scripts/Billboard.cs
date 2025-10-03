using UnityEngine;

/// <summary>
/// Attach to a World Space Canvas (or any GameObject) to rotate only its Y axis
/// so it faces the current active camera. Prefers Canvas.worldCamera, falls back to Camera.main.
/// </summary>
[ExecuteAlways]
public class Billboard : MonoBehaviour
{
    public bool lockX = false;
    public bool lockY = false;
    public bool lockZ = false;
    private Canvas cachedCanvas;

    private void OnEnable()
    {
        cachedCanvas = GetComponent<Canvas>();
        UpdateFacing();
    }

    private void LateUpdate()
    {
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        if (cachedCanvas == null)
        {
            cachedCanvas = GetComponent<Canvas>();
        }

        Camera cam = (cachedCanvas != null ? cachedCanvas.worldCamera : null) 
            ?? Camera.main 
            ?? FindObjectOfType<Camera>();

        if (cam == null)
        {
            return;
        }

        Vector3 toCamera = cam.transform.position - transform.position;

        if (toCamera.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        Vector3 currentEuler = transform.rotation.eulerAngles;
        Vector3 desiredEuler = lookRotation.eulerAngles;

        if (lockX) desiredEuler.x = currentEuler.x;
        if (lockY) desiredEuler.y = currentEuler.y;
        if (lockZ) desiredEuler.z = currentEuler.z;

        transform.rotation = Quaternion.Euler(desiredEuler);
    }
}


