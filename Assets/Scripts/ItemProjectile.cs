using UnityEngine;
using Mirror;

public class ItemProjectile : NetworkBehaviour
{
    [Header("설정")]
    public float speed = 20f;
    public float lifetime = 10f;

    [Header("리소스")]
    public Sprite[] itemSprites; // 인스펙터에서 아이템 타입 순서대로(햄찌, 풀, 대시, 실드 등) 스프라이트 넣기

    [SyncVar] public uint targetNetId;
    [SyncVar(hook = nameof(OnItemTypeChanged))] public ItemType itemType;

    private Transform targetTransform;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 만약 스프라이트 렌더러가 자식에 있다면 GetComponentInChildren 사용
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // 아이템 타입이 변경되면(서버->클라 동기화) 자동으로 실행
    void OnItemTypeChanged(ItemType oldType, ItemType newType)
    {
        if (itemSprites != null && itemSprites.Length > 0)
        {
            // ItemType은 보통 1부터 시작하므로 인덱스 조정 필요 (enum 정의 확인 필요)
            // 여기서는 안전하게 (int)type - 1로 가정하거나, 혹은 직접 매핑
            int index = (int)newType - 1; 
            
            // 인덱스 범위 체크
            if (index >= 0 && index < itemSprites.Length)
            {
                if(spriteRenderer != null) spriteRenderer.sprite = itemSprites[index];
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Invoke(nameof(DestroySelf), lifetime); // 너무 오래 날아가면 삭제
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        FindTarget();
        
        // 이미 데이터가 들어와있을 수 있으므로 한번 호출
        OnItemTypeChanged(itemType, itemType);
    }

    void Update()
    {
        if (isServer)
        {
            ServerUpdate();
        }
        
        // 클라이언트에서는 위치 동기화(NetworkTransform)를 쓰거나 
        // 단순히 시각적으로만 따라가게 할 수도 있지만,
        // 여기서는 서버가 물리/이동을 주도하고 NetworkTransform으로 동기화한다고 가정하거나
        // 간단히 서버에서 이동시키고 위치를 동기화합니다.
        // 만약 NetworkTransform이 없다면 클라이언트도 직접 움직여야 부드럽게 보입니다.
        if (isClient && !isServer)
        {
            ClientUpdate();
        }
    }

    void FindTarget()
    {
        if (targetNetId == 0) return;

        if (NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
        {
            targetTransform = identity.transform;
        }
    }

    void ServerUpdate()
    {
        if (targetTransform == null)
        {
            FindTarget();
            if (targetTransform == null) return; // 타겟을 못 찾음
        }

        // 타겟 방향으로 이동
        Vector3 dir = (targetTransform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // 회전 (화살표처럼 날아가게)
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // 거리 체크 (충돌 감지) - 간단하게 거리로 체크 (Collider 없이도 가능)
        if (Vector3.Distance(transform.position, targetTransform.position) < 1.0f)
        {
            HitTarget();
        }
    }

    void ClientUpdate()
    {
        if (targetTransform == null)
        {
            FindTarget();
            return;
        }

        // 클라이언트 예측 이동 (부드러움을 위해)
        // 실제 판정은 서버가 하므로 시각적 처리만
        Vector3 dir = (targetTransform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    [Server]
    void HitTarget()
    {
        // 타겟에게 효과 적용
        if (targetTransform != null)
        {
            var itemManager = targetTransform.GetComponent<ItemManager>();
            if (itemManager != null)
            {
                itemManager.TargetRpcReceiveAttack(itemType);
            }
        }

        // 자기 자신 삭제
        NetworkServer.Destroy(gameObject);
    }

    void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}
