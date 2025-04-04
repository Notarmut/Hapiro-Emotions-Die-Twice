using UnityEngine;
using Unity.Cinemachine;
using StarterAssets;  // Add this at the top with other using statements

public class CameraLockOn : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float lockOnRadius = 10f;
    [SerializeField] private float maxLockOnDistance = 15f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask obstructionLayer;

    [Header("Input Settings")]
    [SerializeField] private KeyCode lockOnKey = KeyCode.Q;

    [Header("Camera Damping")]
    [SerializeField] private float normalDamping = 0.5f;
    [SerializeField] private float lockedDamping = 0.1f;

    [Header("Rotation Settings")]
    [SerializeField] private float lockOnRotationSpeed = 15f;

    [Header("Player Reference")]
    [SerializeField] private StarterAssets.ThirdPersonController thirdPersonController;

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
        HandleLockOnInput();
        
        if (isLockedOn)
        {
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                ReleaseTarget();
                return;
            }

            UpdatePlayerRotation();
        }
    }

    private void HandleLockOnInput()
    {
        if (Input.GetKeyDown(lockOnKey) || Input.GetKeyDown("joystick button 9"))
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

        // Keep target switching logic unchanged
        if (isLockedOn && Mathf.Abs(Input.GetAxis("Mouse X")) > 0.5f)
        {
            FindPotentialTarget(Input.GetAxis("Mouse X") > 0);
        }
    }

    private void FindPotentialTarget(bool findNext = true)
     {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, lockOnRadius, targetLayer);
        Transform newTarget = null;
        float closestAngle = float.MaxValue;
        float closestDistance = float.MaxValue;

        foreach (var col in hitColliders)
        {
            if (isLockedOn && col.transform == currentTarget) continue;

            Vector3 dirToTarget = (col.transform.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, col.transform.position);
            float angle = Vector3.Angle(transform.forward, dirToTarget);

            if (!isLockedOn && angle > 90f) continue;

            if (Physics.Linecast(transform.position + Vector3.up,
                               col.transform.position + targetOffset,
                               obstructionLayer))
            {
                continue;
            }

            if (isLockedOn)
            {
                Vector3 cross = Vector3.Cross(transform.forward, dirToTarget);
                float relativeAngle = Vector3.Angle(transform.forward, dirToTarget) * (cross.y < 0 ? -1 : 1);

                if ((findNext && relativeAngle > 0 && relativeAngle < closestAngle) ||
                    (!findNext && relativeAngle < 0 && -relativeAngle < closestAngle))
                {
                    closestAngle = Mathf.Abs(relativeAngle);
                    newTarget = col.transform;
                }
            }
            else
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    newTarget = col.transform;
                }
            }
        }

        if (newTarget != null)
        {
            LockOnToTarget(newTarget);
        }
        else if (!isLockedOn)
        {
            Debug.Log("No valid targets found");
        }
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > maxLockOnDistance) return false;

        if (Physics.Linecast(transform.position + Vector3.up,
                           target.position + targetOffset,
                           obstructionLayer))
        {
            return false;
        }

        return true;
    }

    private void LockOnToTarget(Transform target)
    {
        currentTarget = target;
        isLockedOn = true;
        virtualCamera.LookAt = currentTarget;

        // Disable mouse input
        if (thirdPersonController != null)
        {
            thirdPersonController.LockCameraPosition = true;
        }

        composer.m_HorizontalDamping = lockedDamping;
        composer.m_VerticalDamping = lockedDamping;
        composer.m_LookaheadTime = 0f;

        var pov = virtualCamera.GetComponent<CinemachinePOV>();
        pov.m_VerticalAxis.m_MaxSpeed = 0f;
    }

    private void UpdatePlayerRotation()
    {
        if (currentTarget != null)
        {
            Vector3 directionToTarget = currentTarget.position - transform.position;
            directionToTarget.y = 0;

            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    Time.deltaTime * lockOnRotationSpeed);
            }
        }
    }

    private void ReleaseTarget()
    {
        isLockedOn = false;
        currentTarget = null;
        virtualCamera.LookAt = null;

        // Re-enable mouse input
        if (thirdPersonController != null)
        {
            thirdPersonController.LockCameraPosition = false;
        }

        composer.m_HorizontalDamping = normalDamping;
        composer.m_VerticalDamping = normalDamping;
        composer.m_TrackedObjectOffset = Vector3.zero;

        var pov = virtualCamera.GetComponent<CinemachinePOV>();
        pov.m_VerticalAxis.m_MaxSpeed = 2f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, lockOnRadius);

        if (isLockedOn && currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, currentTarget.position + targetOffset);
        }
    }
}