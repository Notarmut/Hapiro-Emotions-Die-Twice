using UnityEngine;
using Unity.Cinemachine;
using StarterAssets;

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
    public GameObject playerarmatureRoot;

    [Header("Locked-On Camera Offset")]
    [SerializeField] private Vector3 lockedOnCameraOffset = new Vector3(0, 2, -5);

    private Transform currentTarget;
    private CinemachineComposer composer;
    private bool isLockedOn;
    private GameObject lookAtTarget;
    private Quaternion previousPlayerRotation;

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

        lookAtTarget = new GameObject("LookAtTarget");
        lookAtTarget.transform.SetParent(null);
        lookAtTarget.hideFlags = HideFlags.HideAndDontSave;

        previousPlayerRotation = transform.rotation;
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
            UpdateLookAtTarget();
            ApplyCameraInversePlayerRotation();
        }
    }

    private void HandleLockOnInput()
    {
        if (Input.GetKeyDown(lockOnKey) || Input.GetKeyDown("joystick button 9"))
        {
            if (!isLockedOn)
                FindPotentialTarget();
            else
                ReleaseTarget();
        }

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
                continue;

            if (isLockedOn)
            {
                Vector3 cross = Vector3.Cross(transform.forward, dirToTarget);
                float relativeAngle = angle * (cross.y < 0 ? -1 : 1);

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
            return false;

        return true;
    }

    private void LockOnToTarget(Transform target)
    {
        currentTarget = target;
        isLockedOn = true;

        virtualCamera.Follow = transform;
        virtualCamera.LookAt = lookAtTarget.transform;

        if (thirdPersonController != null)
        {
            thirdPersonController.LockCameraPosition = true;
        }

        composer.m_HorizontalDamping = lockedDamping;
        composer.m_VerticalDamping = lockedDamping;
        composer.m_LookaheadTime = 0f;

        var pov = virtualCamera.GetComponent<CinemachinePOV>();
        if (pov != null)
        {
            pov.m_VerticalAxis.m_MaxSpeed = 0f;
        }

        previousPlayerRotation = transform.rotation;
    }

    private void UpdateLookAtTarget()
    {
        if (!isLockedOn || currentTarget == null || lookAtTarget == null) return;

        Vector3 midpoint = (transform.position + currentTarget.position + targetOffset) / 2f;
        lookAtTarget.transform.position = midpoint;

        virtualCamera.transform.position = transform.position + lockedOnCameraOffset;

        Quaternion targetRotation = Quaternion.LookRotation(currentTarget.position - virtualCamera.transform.position);
        virtualCamera.transform.rotation = targetRotation;
    }

    private void UpdatePlayerRotation()
    {
        if (currentTarget != null && isLockedOn)
        {
            Vector3 directionToTarget = currentTarget.position - transform.position;
            directionToTarget.y = 0;

            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lockOnRotationSpeed);
            }
        }
    }

    private void ApplyCameraInversePlayerRotation()
    {
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(previousPlayerRotation);
        virtualCamera.transform.rotation = Quaternion.Inverse(deltaRotation) * virtualCamera.transform.rotation;
        previousPlayerRotation = transform.rotation;
    }

    private void ReleaseTarget()
    {
        isLockedOn = false;
        currentTarget = null;

        // Reset camera target to default player camera target
        if (thirdPersonController != null)
        {
            thirdPersonController.LockCameraPosition = false;
            virtualCamera.Follow = thirdPersonController.CinemachineCameraTarget.transform;
            virtualCamera.LookAt = thirdPersonController.CinemachineCameraTarget.transform;
        }

        composer.m_HorizontalDamping = normalDamping;
        composer.m_VerticalDamping = normalDamping;
        composer.m_TrackedObjectOffset = Vector3.zero;

        var pov = virtualCamera.GetComponent<CinemachinePOV>();
        if (pov != null)
        {
            pov.m_HorizontalAxis.m_MaxSpeed = 300f;
            pov.m_VerticalAxis.m_MaxSpeed = 2f;
        }
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