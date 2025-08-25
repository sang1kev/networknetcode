using UnityEngine;

public class CamFollow : MonoBehaviour
{
    public Transform target;

    [SerializeField] private Vector3 offset;
    [SerializeField] private float smoothSpeed = 10f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}
