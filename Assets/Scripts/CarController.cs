using UnityEngine;
using UnityEngine.Tilemaps;
using Mirror;
using System.Collections;

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

    // [수정] 인스펙터에서 볼 수 있게 public으로 두되, 수정은 코드에서만
    public float addedSpeed = 0f;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    private float tileSpeedMultiplier = 1.0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 0; // 구버전 유니티면 drag 사용
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (visualTransform == null && transform.childCount > 0)
            visualTransform = transform.GetChild(0);

        if (groundTilemap == null)
            groundTilemap = GameObject.Find("Tilemap")?.GetComponent<Tilemap>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // 1. 스턴 상태면 입력도 받지 않음 (방향 고정)
        if (isStunned)
        {
            moveDir = Vector2.zero;
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
        // (필요 시 타일 속도 로직 추가)
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

    // ==========================================
    // [중요 수정] 물리 이동 처리 (스턴 로직 강화)
    // ==========================================
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        // 1. 스턴 상태면 강제로 멈춤 (밀림 방지)
        if (isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return; // 아래 이동 코드 실행 안 함
        }

        // 2. 입력이 없으면 멈춤
        if (moveDir == Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 3. 정상 이동 (기본속도 * 타일 + 아이템추가속도)
        float finalSpeed = (moveSpeed * tileSpeedMultiplier) + addedSpeed;
        rb.linearVelocity = moveDir * finalSpeed;
    }

    // ==========================================
    // 아이템 효과 함수들
    // ==========================================

    public bool OnHit(ItemType attackType)
    {
        if (isShieldActive)
        {
            Debug.Log("🛡️ 방어막으로 공격을 막았습니다!");
            isShieldActive = false;
            return false;
        }
        return true;
    }

    public void ApplySpeedBoost(float amount, float duration)
    {
        // 스턴 중에는 부스트 불가
        if (isStunned) return;

        // 기존 부스트가 있다면 멈추고 새로 시작 (중첩 방지)
        StopCoroutine("SpeedBoostRoutine");
        StartCoroutine(SpeedBoostRoutine(amount, duration));
    }

    IEnumerator SpeedBoostRoutine(float amount, float duration)
    {
        addedSpeed = amount; // 속도 더하기
        // Debug.Log($"🚀 부스트! (+{amount})");

        yield return new WaitForSeconds(duration);

        addedSpeed = 0f; // 원상복구
    }

    // [핵심 수정] 스턴 로직 강화
    public void ApplyStun(float duration)
    {
        // 스턴 걸리면 기존 부스트 효과 제거!
        StopCoroutine("SpeedBoostRoutine");
        addedSpeed = 0f;

        // 기존 스턴이 있다면 멈추고 새로 시작 (시간 갱신)
        StopCoroutine("StunRoutine");
        StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        // 물리적으로도 즉시 정지
        rb.linearVelocity = Vector2.zero;

        Debug.Log($"😵 으악! {duration}초간 스턴!");

        yield return new WaitForSeconds(duration);

        isStunned = false;
        Debug.Log("😅 스턴 풀림!");
    }

    public void ActivateShield(float duration)
    {
        StopCoroutine("ShieldRoutine");
        StartCoroutine(ShieldRoutine(duration));
    }

    IEnumerator ShieldRoutine(float duration)
    {
        isShieldActive = true;
        yield return new WaitForSeconds(duration);
        isShieldActive = false;
    }
}