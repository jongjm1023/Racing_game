using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement; // 씬 관리 추가
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
    private SpriteRenderer spriteRenderer; // [NEW] 스프라이트 렌더러 캐싱

    [SyncVar(hook = nameof(OnSkinChanged))] // [NEW] 스킨 이름 동기화
    public string skinName = "";

    [Header("상태 정보 (확인용)")]
    public bool isStunned = false;       // 스턴 상태인가?
    public bool isShieldActive = false;  // 방어막이 켜져있는가?
    private float addedSpeed = 0f; // 아이템으로 추가된 속도 (기본 0) // 아이템으로 인한 속도 변화 (기본 1.0)

    private Rigidbody2D rb;
    private Vector2 moveDir;
    private float tileSpeedMultiplier = 1.0f; // 타일 속도 배율

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckVisibility(scene.name);
    }

    void Start()
    {
        CheckVisibility(SceneManager.GetActiveScene().name);

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (visualTransform == null && transform.childCount > 0)
            visualTransform = transform.GetChild(0);

        if (visualTransform != null)
            spriteRenderer = visualTransform.GetComponent<SpriteRenderer>();

        if (groundTilemap == null)
            groundTilemap = GameObject.Find("Tilemap")?.GetComponent<Tilemap>();

        // [NEW] 내 캐릭터라면, 저장된 스킨을 서버에 알림
        if (isLocalPlayer)
        {
             // MainMenuController에서 저장한 스킨 이름 불러오기
             string savedSkin = PlayerPrefs.GetString("SelectedSkin", "");
             if (!string.IsNullOrEmpty(savedSkin))
             {
                 Debug.Log($"[CarController] 스킨 적용 요청: {savedSkin}");
                 CmdSetSkin(savedSkin);
             }
        }
    }

    // [NEW] 서버에 스킨 변경 요청
    [Command]
    void CmdSetSkin(string newSkinName)
    {
        Debug.Log($"[Server] CmdSetSkin called. Old: {skinName}, New: {newSkinName}");
        skinName = newSkinName; // 서버에서 변경 -> 모든 클라이언트에 OnSkinChanged 호출됨
    }

    // [NEW] 스킨 변경 훅 (모든 클라이언트에서 실행)
    void OnSkinChanged(string oldName, string newName)
    {
        Debug.Log($"[Client] OnSkinChanged: '{oldName}' -> '{newName}'");

        if (string.IsNullOrEmpty(newName)) return;

        // 리소스에서 스프라이트 로드
        Sprite sprite = Resources.Load<Sprite>(newName);
        
        if (sprite != null)
        {
            // 아직 Start()가 안 돌아서 변수가 비었을 수 있음 -> 직접 찾기
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                // Debug.Log($"[Client] 스프라이트 적용 성공: {newName}");
            }
            else
            {
                Debug.LogError("[Client] SpriteRenderer를 자식 오브젝트에서 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError($"[Client] Resources.Load 실패! 이름: '{newName}' (파일이 Resources 폴더에 있는지, 오타가 없는지 확인하세요)");
        }
    }

    // 씬 이름에 따라 숨기기/보이기 결정
    void CheckVisibility(string sceneName)
    {
        bool isGameScene = (sceneName == "SampleScene"); // 게임 씬 체크

        // 1. 렌더러 숨기기
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = isGameScene;
        }

        // 2. 콜라이더 끄기
        foreach (var c in GetComponentsInChildren<Collider2D>())
        {
            c.enabled = isGameScene;
        }
        
        // 3. 스크립트 비활성화 (업데이트 멈춤) - 단, StopImmediately 호출에는 영향 없게 주의
        // 여기서는 물리/입력 업데이트만 막기 위해 this.enabled를 씁니다.
        this.enabled = isGameScene; 
    }

    [SyncVar] private float syncedRotationAngle; // [NEW] 서버에서 관리하는 회전 각도

    void Update()
    {
        // 1. 로컬 플레이어 (내가 조종)
        if (isLocalPlayer)
        {
            if (isStunned)
            {
                moveDir = Vector2.zero;
                return;
            }

            moveDir = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveDir += Vector2.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveDir += Vector2.right;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveDir += Vector2.up;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveDir += Vector2.down;

            moveDir = moveDir.normalized;

            // [DEBUG] F4 누르면 속도 2배
            if (Input.GetKeyDown(KeyCode.F4))
            {
                moveSpeed *= 2f;
                Debug.Log($"[DEBUG] 속도 2배 증가! 현재 속도: {moveSpeed}");
            }
            // [DEBUG] F5 누르면 속도 절반
            if (Input.GetKeyDown(KeyCode.F5))
            {
                moveSpeed *= 0.5f;
                Debug.Log($"[DEBUG] 속도 절반 감소! 현재 속도: {moveSpeed}");
            }

            UpdateTileSpeed();

            if (moveDir != Vector2.zero)
            {
                HandleVisualRotation(moveDir);
                
                // [NEW] 내 각도를 서버로 전송 (다른 사람들도 보라고)
                float currentAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(currentAngle - syncedRotationAngle) > 1f) // 변경이 있을 때만 전송
                {
                    CmdUpdateRotation(currentAngle);
                }
            }
        }
        // 2. 리모트 플레이어 (친구가 조종)
        else
        {
            // [NEW] 서버에서 온 각도로 부드럽게 회전
            Quaternion targetRotation = Quaternion.Euler(0, 0, syncedRotationAngle + -90f); // -90f 보정 주의
            visualTransform.rotation = Quaternion.RotateTowards(visualTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // [NEW] 각도 동기화 명령
    [Command]
    void CmdUpdateRotation(float angle)
    {
        syncedRotationAngle = angle;
    }

    private void UpdateTileSpeed()
    {
        if (groundTilemap == null) return;

        Vector3Int cellPos = groundTilemap.WorldToCell(transform.position);
        TileBase tile = groundTilemap.GetTile(cellPos);

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