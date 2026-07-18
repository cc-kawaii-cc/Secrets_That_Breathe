using UnityEngine;

public class FlashlightSystem : MonoBehaviour
{
    [Header("Settings")]
    public Light flashlightSource;
    public float maxDistance = 5f;
    public LayerMask clueLayer;
    void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, maxDistance, clueLayer);
        foreach (Collider hit in hits)
        {
            Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            if (angle < flashlightSource.spotAngle / 2)
            {
                if (Physics.Raycast(transform.position, directionToTarget, out RaycastHit rayHit, maxDistance))
                {
                    if (rayHit.collider == hit)
                    {
                        hit.GetComponent<ClueObject>()?.OnLightEnter();
                        continue;
                    }
                }
            }
            hit.GetComponent<ClueObject>()?.OnLightExit();
        }
    }
}