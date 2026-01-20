using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    public Transform target;

    [Header("줌 설정 (2D)")]
    public float defaultSize = 9f;    // 평소 화면 크기
    public float boostSize = 8f;      // Z키 누를 때 화면 크기 (값이 클수록 멀리 보임)
    public float zoomSpeed = 5.0f;    // 크기 전환 속도

    [Header("위치 설정")]
    public float followSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10); // 2D에서는 Z값이 보통 -10

    private Camera cam;
    private bool isInShop = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        // 카메라 설정을 자동으로 2D Orthographic으로 변경
        cam.orthographic = true;
        cam.orthographicSize = defaultSize;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 위치 추적 (Z값은 유지하고 X, Y만 부드럽게 따라감)
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed);

        // 2. 줌 상태에 따른 Orthographic Size 변경 (외부에서 SetZoom 호출)
        float targetSize = isZoomed ? boostSize : defaultSize;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
    }

    private bool isZoomed = false;

    // [NEW] 외부(CarController)에서 줌 제어
    public void SetZoom(bool zoom)
    {
        isZoomed = zoom;
    }

    void Update()
    {

    }

}