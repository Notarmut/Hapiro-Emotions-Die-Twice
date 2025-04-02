using UnityEngine;
using Unity.Cinemachine;

public class CameraLockOn : MonoBehaviour
{
    [Header("Cinemachine Settings")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float lockOnRadius = 10f;
    [SerializeField] private float maxLockOnDistance = 15f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Input Settings")]
    [SerializeField] private KeyCode lockOnKey = KeyCode.Q;
    
    private Transform currentTarget;
    private CinemachineComposer composer;
    private bool isLockedOn;

    private void Start()
    {
        if (virtualCamera == null)
        {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (virtualCamera == null)
            {
                Debug.LogError("No CinemachineVirtualCamera found in scene!");
                enabled = false;
                return;
            }
        }

        composer = virtualCamera.GetCinemachineComponent<CinemachineComposer>();
        if (composer == null)
        {
            Debug.LogError("No CinemachineComposer found on virtual camera!");
            enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(lockOnKey))
        {
            ToggleLockOn();
        }

        if (isLockedOn)
        {
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                ReleaseTarget();
                return;
            }

            UpdateCameraTracking();
        }
    }

    private void ToggleLockOn()
    {
        if (!isLockedOn)
        {
            FindPotentialTarget();
        }
        else
        {
            ReleaseTarget();
        }
    }

    private void FindPotentialTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, lockOnRadius);
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (var col in hitColliders)
        {
            if (col.CompareTag("Enemy"))
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance && distance <= maxLockOnDistance)
                {
                    closestDistance = distance;
                    closestTarget = col.transform;
                }
            }
        }

        if (closestTarget != null)
        {
            LockOnToTarget(closestTarget);
            Debug.Log("Locked onto: " + closestTarget.name);
        }
        else
        {
            Debug.Log("No valid targets found");
        }
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;
        
        // Check if target is within max distance
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > maxLockOnDistance) return false;

        // Check line of sight
        Vector3 direction = (target.position + targetOffset - virtualCamera.transform.position).normalized;
        if (Physics.Raycast(virtualCamera.transform.position, direction, out RaycastHit hit, maxLockOnDistance))
        {
            if (hit.transform != target)
            {
                return false;
            }
        }

        return true;
    }

    private void LockOnToTarget(Transform target)
    {
        currentTarget = target;
        isLockedOn = true;
        virtualCamera.LookAt = currentTarget;
        
        // Tighten camera damping for precise tracking
        composer.m_HorizontalDamping = 0.1f;
        composer.m_VerticalDamping = 0.1f;
        composer.m_LookaheadTime = 0f;
    }

    private void UpdateCameraTracking()
    {
        // Force camera to look exactly at target
        Vector3 targetPos = currentTarget.position + targetOffset;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(targetPos);
        Vector3 screenCenter = new Vector3(Screen.width/2, Screen.height/2, 0);
        
        // Adjust composer offset to keep target centered
        Vector3 offset = (screenPoint - screenCenter) / 50f;
        composer.m_TrackedObjectOffset = new Vector3(-offset.x, offset.y, 0);
    }

    private void ReleaseTarget()
    {
        isLockedOn = false;
        currentTarget = null;
        virtualCamera.LookAt = null;
        
        // Reset camera damping
        composer.m_HorizontalDamping = 0.5f;
        composer.m_VerticalDamping = 0.5f;
        composer.m_TrackedObjectOffset = Vector3.zero;
        Debug.Log("Lock-on released");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, lockOnRadius);
    }
}