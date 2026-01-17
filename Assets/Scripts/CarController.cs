using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement; // 씬 관리 추가
using Mirror;

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

    private Rigidbody2D rb;
    private Vector2 moveDir;
    private float currentSpeedMultiplier = 1.0f;

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

    void Update()
    {
        if (!isLocalPlayer) return;

        // 1. 입력 받기
        moveDir = Vector2.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveDir += Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveDir += Vector2.right;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveDir += Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveDir += Vector2.down;

        moveDir = moveDir.normalized;

        // 2. 타일 체크
        UpdateTileSpeed();

        // 3. 스프라이트 회전
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

        if (tile is RoadTile roadTile)
        {
            currentSpeedMultiplier = roadTile.speedMultiplier;
        }
        else
        {
            currentSpeedMultiplier = 0.5f;
        }
    }

    private void HandleVisualRotation(Vector2 dir)
    {
        if (visualTransform == null) return;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float offset = -90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle + offset);
        visualTransform.rotation = Quaternion.RotateTowards(visualTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // [중요] 카메라 로직은 LateUpdate에서 처리하는 것이 가장 떨림이 적고 확실합니다.
    void LateUpdate()
    {
        // "나"인 경우에만 카메라를 내 위치로 고정
        if (isLocalPlayer && Camera.main != null)
        {
            Vector3 targetPos = transform.position;
            targetPos.z = -10f; // 카메라 거리 확보
            Camera.main.transform.position = targetPos;
        }
    }

    // CarController2D.cs 내부

    // 기존 FixedUpdate 윗부분에 추가
    public void StopImmediately()
    {
        // 입력을 0으로 초기화
        moveDir = Vector2.zero;

        // 리지드바디 속도 즉시 제거
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true; // 물리 영향 차단 (선택 사항)
        }

        // 스크립트 비활성화
        this.enabled = false;
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        // moveDir가 Vector2.zero일 때 멈추는 로직이 이미 있지만, 
        // 확실하게 하기 위해 위 함수를 호출하는 것이 좋습니다.
        if (moveDir == Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float finalSpeed = moveSpeed * currentSpeedMultiplier;
        rb.linearVelocity = moveDir * finalSpeed;
    }
}