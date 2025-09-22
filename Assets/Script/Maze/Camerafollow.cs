using UnityEngine;

public class Camerafollow : MonoBehaviour
{
    [Header("필수")]
    public Transform target;                     // 플레이어 Transform

    [Header("옵션")]
    public Vector3 offset = new Vector3(0, 0, -10f); // 카메라 z는 반드시 음수(예: -10)
    public float smoothTime = 0.12f;             // 부드럽게 따라오는 시간(0이면 즉시)
    public bool keepPlayerCentered = true;       // 항상 중앙 고정

    private Vector3 _velocity;

    void Reset()
    {
        // 카메라가 Orthographic 2D면 이렇게 설정 권장
        var cam = GetComponent<Camera>();
        if (cam != null) cam.orthographic = true;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 중앙 고정: 목표 위치 = 플레이어 위치 + 오프셋
        Vector3 targetPos = target.position + offset;

        // 부드럽게 이동 (즉시 원하면 smoothTime=0으로 두고 아래 한 줄만 쓰기)
        if (smoothTime > 0f)
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
        else
            transform.position = targetPos;

        // Z 고정(혹시 다른 스크립트가 변경해도 보정)
        if (transform.position.z != offset.z)
            transform.position = new Vector3(transform.position.x, transform.position.y, offset.z);
    }
}
