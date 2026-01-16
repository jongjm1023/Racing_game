using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody2D))]
public class CarController2D : NetworkBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 10f;

    private Rigidbody2D rb;
    private Vector2 moveDir;

    public override void OnStartLocalPlayer()
    {
        GetComponent<SpriteRenderer>().color = Color.green;

        // 🎥 시작 시 카메라 위치 강제 세팅 (기존 코드 유지)
        if (Camera.main != null)
        {
            Camera.main.transform.position =
                new Vector3(transform.position.x, transform.position.y, -10f);
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 0f;

        // 회전은 코드로만 제어
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        moveDir = Vector2.zero;

        // ⬅️➡️⬆️⬇️ 절대 좌표 입력
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveDir += Vector2.left;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveDir += Vector2.right;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveDir += Vector2.up;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveDir += Vector2.down;

        moveDir = moveDir.normalized;
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        if (moveDir == Vector2.zero)
            return;

        Vector2 targetPos =
            rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(targetPos);
    }

    // 🎥 카메라 따라가기 (기존 로직 그대로)
    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        if (Camera.main != null)
        {
            Vector3 targetPos = transform.position;
            targetPos.z = -10f;
            Camera.main.transform.position = targetPos;
        }
    }
}
