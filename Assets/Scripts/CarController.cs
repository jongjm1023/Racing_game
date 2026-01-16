using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody2D))]
public class CarController2D : NetworkBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 12f;
    public float rotationSpeed = 20f; // 회전 반응 속도

    [Header("그래픽 설정")]
    // 스프라이트가 원래 어디를 보고 있는지에 따라 조절 (0, -90, 90, 180 중 하나)
    // 차 그림이 위를 보고 있다면 0 또는 -90을 시도해보세요.
    public float spriteOffset = -90f;

    [Header("스킬 설정")]
    public float dashForce = 8f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    public override void OnStartLocalPlayer()
    {
        Camera.main.GetComponent<CameraFollow>().target = transform;
        GetComponent<SpriteRenderer>().color = Color.green;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 0f;

        // 🚨 중요: 물리 충돌로 인해 차가 뱅글뱅글 도는 것을 막습니다.
        // 회전은 오직 스크립트로만 제어합니다.
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // 1️⃣ [입력] 화면 기준 절대 좌표 입력 (Local 아님!)
        // 차의 회전값(transform.rotation)을 전혀 곱하지 않습니다.
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(x, y).normalized;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            CmdDash();
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        Move();
    }

    void Move()
    {
        // 2️⃣ [이동] 키보드 방향 그대로 속도에 꽂아넣기
        // 차가 180도 돌아있어도 moveInput이 (0, 1)이면 무조건 위로 갑니다.
        if (moveInput.magnitude < 0.1f)
        {
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.linearVelocity = moveInput * moveSpeed; // 👈 여기가 핵심 (절대 이동)

            // 3️⃣ [회전] 이동은 이동대로 하고, 차의 '그림'만 진행 방향을 보게 돌림
            RotateSpriteToDirection();
        }
    }

    void RotateSpriteToDirection()
    {
        // "이동하는 방향(moveInput)"을 바라보게 각도 계산
        float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;

        // 스프라이트 머리 방향 보정
        targetAngle += spriteOffset;

        // 부드럽게 회전 (Lerp)
        // 만약 이것도 답답하면 rb.rotation = targetAngle; 로 바꾸면 칼같이 돕니다.
        rb.rotation = Mathf.LerpAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
    }

    [Command]
    void CmdDash()
    {
        RpcDashEffect();
    }

    [ClientRpc]
    void RpcDashEffect()
    {
        // 대시는 "현재 이동 중인 방향"으로 힘을 가함
        // 멈춰있을 땐 차가 보는 방향(transform.up)으로
        Vector2 dashDir = rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.normalized : (Vector2)transform.up;

        rb.AddForce(dashDir * dashForce, ForceMode2D.Impulse);
    }
}