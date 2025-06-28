using UnityEngine;

public class DunyanınSonuMustafaBosstest : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform player; // Oyuncu transformu
    public Transform head;   // Boss'un kafa kısmı (dönecek)

    [Header("Dönüş Ayarları")]
    public float rotationSpeed = 5f;
    public float headLookSpeed = 5f;
    public float maxHeadAngle = 45f;

    [Header("Mesafe Kontrolleri")]
    public float minLookDistance = 0.5f; // Çok yaklaştığında boss dönmeye çalışmasın

    void Update()
    {
        if (player == null || head == null) return;

        // --- Ana Gövde Dönüşü (Y ekseninde, yatay) ---
        Vector3 direction = player.position - transform.position;
        direction.y = 0; // Yalnızca yatay dönüş

        if (direction.sqrMagnitude >= minLookDistance * minLookDistance)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            Quaternion correctedRotation = lookRotation * Quaternion.Euler(0, 180f, 0); // Eğer modelin yönü tersse
            transform.rotation = Quaternion.Slerp(transform.rotation, correctedRotation, rotationSpeed * Time.deltaTime);
        }

        // --- Kafa Takibi (X ekseninde eğilme) ---
        Vector3 headDir = player.position - head.position;

        if (headDir.sqrMagnitude > 0.001f)
        {
            Quaternion headLookRotation = Quaternion.LookRotation(headDir);
            headLookRotation *= Quaternion.Euler(0, 180f, 0); // Eğer kafa da ters bakıyorsa

            Quaternion clampedRotation = ClampHeadRotation(headLookRotation);
            head.rotation = Quaternion.Slerp(head.rotation, clampedRotation, headLookSpeed * Time.deltaTime);
        }
    }

    // --- Kafa rotasyonunu sınırlamak için ---
    Quaternion ClampHeadRotation(Quaternion targetRot)
    {
        Vector3 euler = targetRot.eulerAngles;

        // Kafa yalnızca yukarı-aşağı eğilsin, yönü boss ile aynı kalsın
        euler.y = transform.eulerAngles.y;
        euler.z = 0;

        float angleX = Mathf.DeltaAngle(0, euler.x);
        angleX = Mathf.Clamp(angleX, -maxHeadAngle, maxHeadAngle);
        euler.x = angleX;

        return Quaternion.Euler(euler);
    }
}
