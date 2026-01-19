using UnityEngine;
using UnityEngine.Tilemaps;
using Mirror;
using System.Collections; // 코루틴 사용을 위해 필수

[RequireComponent(typeof(Rigidbody2D))]
public class CarController2D : NetworkBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 720f;
    public Tilemap groundTilemap;

    [Header("시각적 회전 대상")]
    public Transform visualTransform;

    [Header("상태 정보 (확인용)")]
    public bool isStunned = false;       // 스턴 상태인가?
    public bool isShieldActive = false;  // 방어막이 켜져있는가?
    private float addedSpeed = 0f; // 아이템으로 추가된 속도 (기본 0) // 아이템으로 인한 속도 변화 (기본 1.0)

    private Rigidbody2D rb;
    private Vector2 moveDir;
    private float tileSpeedMultiplier = 1.0f; // 타일 속도 배율

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (visualTransform == null && transform.childCount > 0)
            visualTransform = transform.GetChild(0);

        if (groundTilemap == null)
            groundTilemap = GameObject.Find("Tilemap")?.GetComponent<Tilemap>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // 1. 스턴 상태면 입력 차단
        if (isStunned)
        {
            moveDir = Vector2.zero; // 이동 방향 초기화
            return;
        }

        // 2. 입력 받기
        moveDir = Vector2.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveDir += Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveDir += Vector2.right;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveDir += Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveDir += Vector2.down;

        moveDir = moveDir.normalized;

        // 3. 타일 체크
        UpdateTileSpeed();

        // 4. 스프라이트 회전
        if (moveDir != Vector2.zero)
        {
            HandleVisualRotation(moveDir);
        }
    }

    private void UpdateTileSpeed()
    {
        if (groundTilemap == null) return;

        Vector3Int cellPos = groundTilemap.WorldToCell(transform.position);
        TileBase tile = groundTilemap.GetTile(cellPos);

        // RoadTile 클래스가 있다면 사용, 없으면 태그나 이름으로 체크 가능
        // 여기선 예시로 유지
        // if (tile is RoadTile roadTile) tileSpeedMultiplier = roadTile.speedMultiplier;
        // else tileSpeedMultiplier = 0.5f;

        // (임시) 타일 로직이 없다면 기본 1.0
        tileSpeedMultiplier = 1.0f;
    }

    private void HandleVisualRotation(Vector2 dir)
    {
        if (visualTransform == null) return;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float offset = -90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle + offset);
        visualTransform.rotation = Quaternion.RotateTowards(visualTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (isLocalPlayer && Camera.main != null)
        {
            Vector3 targetPos = transform.position;
            targetPos.z = -10f;
            Camera.main.transform.position = targetPos;
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        if (isStunned || moveDir == Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // [수정된 공식] (기본속도 * 타일배율) + 아이템추가속도
        float finalSpeed = (moveSpeed * tileSpeedMultiplier) + addedSpeed;

        rb.linearVelocity = moveDir * finalSpeed;
    }

    // ==========================================
    // 여기서부터 아이템 효과 관련 함수들 추가
    // ==========================================

    // 1. 공격 당했을 때 (ItemManager에서 호출)
    public bool OnHit(ItemType attackType)
    {
        if (isShieldActive)
        {
            Debug.Log("방어막으로 막음!");
            isShieldActive = false; // 방어막 소모
            return false; // 공격 실패함
        }
        return true; // 공격 성공함
    }

    // 2. 속도 부스트 (대쉬, 햄찌 성공)
    public void ApplySpeedBoost(float amount, float duration)
    {
        StartCoroutine(SpeedBoostRoutine(amount, duration));
    }

    IEnumerator SpeedBoostRoutine(float amount, float duration)
    {
        // [변경] 단순히 속도를 더해줍니다. (예: 10 + 5 = 15)
        addedSpeed = amount;

        // UI나 로그로 확인하고 싶다면
        // Debug.Log($"부스트 온! 현재 추가 속도: {addedSpeed}");

        yield return new WaitForSeconds(duration);

        addedSpeed = 0f; // 원상복구
                         // Debug.Log("부스트 종료");
    }

    // 3. 스턴 (햄찌 실패)
    public void ApplyStun(float duration)
    {
        StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        Debug.Log("으악! 스턴!");

        yield return new WaitForSeconds(duration);

        isStunned = false;
        Debug.Log("스턴 풀림");
    }

    // 4. 방어막 활성
    public void ActivateShield(float duration)
    {
        StartCoroutine(ShieldRoutine(duration));
    }

    IEnumerator ShieldRoutine(float duration)
    {
        isShieldActive = true;
        yield return new WaitForSeconds(duration);
        isShieldActive = false;
    }
}