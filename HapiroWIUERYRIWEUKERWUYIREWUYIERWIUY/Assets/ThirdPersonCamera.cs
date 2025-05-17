using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2f, -4f);
    public float mouseSensitivity = 3f;
    public float distance = 5f;
    public float minY = -35f;
    public float maxY = 60f;

    public float lockOnRange = 15f;

    [Header("Lock-On Offset Controls")]
    public float lockOnYawOffset = 0f;
    public float lockOnPitchOffset = 0f;
    public float lockOnHeightOffset = 1.5f;

    private Transform lockOnTarget;
    private bool isLockedOn = false;

    private float yaw;
    private float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        HandleLockOnToggle();

        if (isLockedOn && lockOnTarget != null)
        {
            // Lock-on behavior
            Vector3 direction = lockOnTarget.position - target.position;
            direction.y = 0f;
            Quaternion baseRotation = Quaternion.LookRotation(direction);

            yaw = baseRotation.eulerAngles.y + lockOnYawOffset;
            pitch = lockOnPitchOffset;
            pitch = Mathf.Clamp(pitch, minY, maxY);

            Quaternion lockRotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = target.position + lockRotation * offset;
            transform.LookAt(lockOnTarget.position + Vector3.up * lockOnHeightOffset);
        }
        else
        {
            // Free camera
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minY, maxY);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = target.position + rotation * offset;
            transform.LookAt(target.position + Vector3.up * 1.5f);
        }
    }

    void HandleLockOnToggle()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isLockedOn)
            {
                isLockedOn = false;
                lockOnTarget = null;
                Debug.Log("Lock-on disabled.");
            }
            else
            {
                lockOnTarget = FindNearestEnemy();
                if (lockOnTarget != null)
                {
                    isLockedOn = true;
                    Debug.Log("Locked on to: " + lockOnTarget.name);
                }
                else
                {
                    Debug.Log("No enemy found in range.");
                }
            }
        }
    }

    Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(target.position, enemy.transform.position);
            if (dist < lockOnRange && dist < closestDistance)
            {
                closestDistance = dist;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    public bool IsLockedOn() => isLockedOn;
    public Transform GetLockOnTarget() => lockOnTarget;
}
