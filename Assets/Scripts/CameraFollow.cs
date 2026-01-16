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

    [Header("상점 설정")]
    public Transform racingCamPos;  // 레이싱 카메라 위치
    public Transform shopCamPos;    // 상점 카메라 위치
    public Canvas shopCanvas;       // 상점 Canvas
    public ShopManager shopManager; // 상점 매니저

    private Camera cam;
    private bool isInShop = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        // 카메라 설정을 자동으로 2D Orthographic으로 변경
        cam.orthographic = true;
        cam.orthographicSize = defaultSize;

        // 초기: 레이싱 모드
        if (racingCamPos != null)
        {
            transform.position = racingCamPos.position;
            transform.rotation = racingCamPos.rotation;
        }
        if (shopCanvas != null)
            shopCanvas.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (isInShop)
        {
            // 상점 모드: 카메라 고정
            if (shopCamPos != null)
            {
                transform.position = shopCamPos.position;
                transform.rotation = shopCamPos.rotation;
            }
            return;
        }

        if (target == null) return;

        // 1. 위치 추적 (Z값은 유지하고 X, Y만 부드럽게 따라감)
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed);

        // 2. Z키 입력에 따른 Orthographic Size 변경 (화면 크기 확대/축소)
        float targetSize = Input.GetKey(KeyCode.Z) ? boostSize : defaultSize;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
    }

    void Update()
    {
        // S 키로 상점 토글
        if (Input.GetKeyDown(KeyCode.S))
        {
            ToggleShop();
        }
    }

    public void ToggleShop()
    {
        isInShop = !isInShop;
        if (isInShop)
        {
            // 상점으로 이동
            if (shopCamPos != null)
            {
                transform.position = shopCamPos.position;
                transform.rotation = shopCamPos.rotation;
            }
            if (shopCanvas != null)
                shopCanvas.gameObject.SetActive(true);
            if (shopManager != null)
                shopManager.StartCoroutine(shopManager.LoadShopData());
        }
        else
        {
            // 레이싱으로 돌아감
            if (racingCamPos != null)
            {
                transform.position = racingCamPos.position;
                transform.rotation = racingCamPos.rotation;
            }
            if (shopCanvas != null)
                shopCanvas.gameObject.SetActive(false);
        }
    }
}