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

    [Header("Targeting UI")]
    public GameObject lockOnIndicatorPrefab; // Small dot UI prefab
    private GameObject lockOnIndicator;
    private Canvas lockOnCanvas;

    private Transform lockOnTarget;
    private bool isLockedOn = false;

    private float yaw;
    private float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        // Create targeting UI
        CreateLockOnIndicator();
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
            
            // Update lock-on indicator position
            UpdateLockOnIndicator();
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
            
            // Hide indicator when not locked on
            if (lockOnIndicator != null) lockOnIndicator.SetActive(false);
        }
    }

    void CreateLockOnIndicator()
    {
        // Create canvas for indicator
        lockOnCanvas = new GameObject("LockOnCanvas").AddComponent<Canvas>();
        lockOnCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        lockOnCanvas.sortingOrder = 1000;
        
        // Instantiate indicator
        if (lockOnIndicatorPrefab != null)
        {
            lockOnIndicator = Instantiate(lockOnIndicatorPrefab, lockOnCanvas.transform);
            lockOnIndicator.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Lock On Indicator Prefab not assigned!");
        }
    }

    void UpdateLockOnIndicator()
    {
        if (lockOnIndicator == null || lockOnTarget == null) return;
        
        // Convert enemy position to screen space
        Vector3 screenPos = Camera.main.WorldToScreenPoint(lockOnTarget.position);
        
        // Check if enemy is in front of camera
        if (screenPos.z > 0)
        {
            lockOnIndicator.SetActive(true);
            lockOnIndicator.transform.position = screenPos;
        }
        else
        {
            lockOnIndicator.SetActive(false);
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
                if (lockOnIndicator != null) lockOnIndicator.SetActive(false);
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