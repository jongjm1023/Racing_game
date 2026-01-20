using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class ItemManager : NetworkBehaviour
{
    [Header("참조")]
    public CarController2D carController;
    // public CarController2D enemyCarController; // (네트워크에서는 직접 참조보다 Connection을 찾는 방식이 안전하므로 주석 처리하거나 무시합니다)

    // 아이템 저장소
    public Queue<ItemType> itemQueue = new Queue<ItemType>();

    // UI 변수
    private Image slot1Image;
    private Image slot2Image;
    private GameObject grassEffectUI;
    private GameObject qtePanel;
    private RectTransform qteCursor;

    [Header("미니게임 상태")]
    public bool isQteActive = false;
    private float qteCursorPos = 0f;
    private float qteDirection = 1f;
    private float qteTimer = 0f;

    [Header("리소스")]
    public Sprite[] inputItemSprites;

    void Start()
    {
        if (!isLocalPlayer) return;

        isQteActive = false;
        itemQueue.Clear();

        Debug.Log("🔄 ItemManager 초기화 완료.");

        if (UIManager.Instance != null)
        {
            slot1Image = UIManager.Instance.slot1;
            slot2Image = UIManager.Instance.slot2;
            grassEffectUI = UIManager.Instance.grassPanel;
            qtePanel = UIManager.Instance.hamsterPanel;
            if (qtePanel) qteCursor = qtePanel.transform.Find("Cursor")?.GetComponent<RectTransform>();

            if (slot1Image) slot1Image.enabled = false;
            if (slot2Image) slot2Image.enabled = false;
        }

        if (carController == null) carController = GetComponent<CarController2D>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // 아이템 사용 (Z키)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (isQteActive) return;
            if (itemQueue.Count == 0) return;

            UseItem();
        }

        // 미니게임 업데이트
        if (isQteActive) UpdateHamsterQTE();

        // [테스트용 치트] 1, 2, 3, 4번 키로 아이템 획득
        if (Input.GetKeyDown(KeyCode.Alpha1)) AddItem(ItemType.HamsterBomb);
        if (Input.GetKeyDown(KeyCode.Alpha2)) AddItem(ItemType.GrassField);
        if (Input.GetKeyDown(KeyCode.Alpha3)) AddItem(ItemType.DashBoom);
        if (Input.GetKeyDown(KeyCode.Alpha4)) AddItem(ItemType.Shield);
    }

    public void AddItem(ItemType newItem)
    {
        if (itemQueue.Count >= 2) return;
        itemQueue.Enqueue(newItem);
        UpdateItemUI();
    }

    // ==========================================
    // 📡 [네트워크 핵심] 아이템 사용 분기점
    // ==========================================
    void UseItem()
    {
        if (itemQueue.Count > 0)
        {
            ItemType usedItem = itemQueue.Dequeue();
            UpdateItemUI(); // UI 즉시 갱신

            // 공격 아이템인지 버프 아이템인지 판단
            if (usedItem == ItemType.HamsterBomb || usedItem == ItemType.GrassField)
            {
                // [공격] 서버로 명령을 보냄 (내가 아니라 적에게 발동해야 함)
                Debug.Log($"⚔️ 공격 아이템 사용: {usedItem} -> 적에게 전송!");
                CmdAttackEnemy(usedItem);
            }
            else
            {
                // [버프] 나 자신에게 즉시 발동
                Debug.Log($"🛡️ 버프 아이템 사용: {usedItem} -> 나에게 적용!");
                ExecuteEffectLocal(usedItem);
            }
        }
    }


    // 1. [Command] 서버야, 나(보낸 사람) 말고 다른 애들한테 공격 날려줘!
    [Command]
    void CmdAttackEnemy(ItemType type)
    {
        // 내 고유 번호 (Network ID)
        uint myNetId = this.netId;
        int attackCount = 0;

        Debug.Log($"[Server] 📡 공격 요청 수신! (공격자 ID: {myNetId})");

        // 서버에 접속한 모든 '연결(사람)'을 뒤짐
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            // 접속자의 플레이어 캐릭터가 존재하는지 확인
            if (conn.identity != null)
            {
                // 그 사람의 ID가 내 ID와 다르다면? => 적이다!
                if (conn.identity.netId != myNetId)
                {
                    // [FIX] 상대방의 ItemManager 컴포넌트를 찾아서, '그 객체'에게 RPC를 보내야 함
                    var targetItemManager = conn.identity.GetComponent<ItemManager>();
                    if (targetItemManager != null)
                    {
                        Debug.Log($"[Server] 🎯 타겟 발견! (타겟 ID: {conn.identity.netId}) -> 공격 발사!");
                        
                        // [TargetRpc]는 호출된 인스턴스의 소유자(Client)에게 전송됩니다.
                        // targetItemManager는 상대방 플레이어의 오브젝트이므로, 
                        // 여기서 함수를 부르면 상대방 컴퓨터에서 실행됩니다.
                        targetItemManager.TargetRpcReceiveAttack(type);
                        attackCount++;
                    }
                }
            }
        }

        if (attackCount == 0)
        {
            Debug.Log("[Server] ❌ 공격할 상대를 찾지 못했습니다. (혼자 있거나 상대방 로딩 덜 됨)");
        }
    }

    // 2. [TargetRpc] 타겟이 된 클라이언트에서 실행
    // 인자에서 NetworkConnection을 제거 (호출 주체가 곧 타겟이므로)
    [TargetRpc]
    public void TargetRpcReceiveAttack(ItemType type)
    {
        if(carController.OnHit()){
            ExecuteEffectLocal(type);
            Debug.Log($"💥 [Client] 공격 아이템 피격! ({type}) -> 효과 발동!");
        }
    }

    // 실질적인 효과 실행 (나한테 쓰든, 남이 나한테 썼든 여기서 처리)
    void ExecuteEffectLocal(ItemType type)
    {
        switch (type)
        {
            case ItemType.HamsterBomb: StartHamsterQTE(); break;       // 적에게 QTE 띄움
            case ItemType.GrassField: StartCoroutine(ShowGrassField()); break; // 적 화면 가림
            case ItemType.DashBoom: carController.ApplySpeedBoost(15f, 2f); break; // 내 속도 증가
            case ItemType.Shield: carController.ActivateShield(3f); break;    // 내 쉴드 켜기
        }
    }

    // ==========================================
    //  UI 갱신
    // ==========================================
    void UpdateItemUI()
    {
        if (slot1Image == null || slot2Image == null) return;
        ItemType[] items = itemQueue.ToArray();

        slot1Image.enabled = items.Length >= 1;
        if (items.Length >= 1) slot1Image.sprite = inputItemSprites[(int)items[0] - 1];

        slot2Image.enabled = items.Length >= 2;
        if (items.Length >= 2) slot2Image.sprite = inputItemSprites[(int)items[1] - 1];
    }

    // ==========================================
    // 🐹 햄찌 미니게임 로직 (변경 없음)
    // ==========================================
    void StartHamsterQTE()
    {
        if (qtePanel == null) return;
        isQteActive = true;
        qtePanel.SetActive(true);
        foreach (Transform child in qtePanel.transform) child.gameObject.SetActive(true);
        qtePanel.transform.SetAsLastSibling();

        qteTimer = 3.0f;
        qteCursorPos = 0f;
        qteDirection = 1f;
    }

    void UpdateHamsterQTE()
    {
        qteCursorPos += Time.deltaTime * 2.0f * qteDirection;
        if (qteCursorPos >= 1f) { qteCursorPos = 1f; qteDirection = -1f; }
        if (qteCursorPos <= 0f) { qteCursorPos = 0f; qteDirection = 1f; }

        if (qteCursor != null) qteCursor.anchoredPosition = new Vector2((qteCursorPos - 0.5f) * 300f, 0);

        qteTimer -= Time.deltaTime;
        if (qteTimer <= 0) EndHamsterQTE(false);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndHamsterQTE(qteCursorPos >= 0.4f && qteCursorPos <= 0.6f);
        }
    }

    void EndHamsterQTE(bool success)
    {
        isQteActive = false;
        if (qtePanel) qtePanel.SetActive(false);

        if (success)
        {
            Debug.Log("🎉 방어 성공! 부스트!");
            carController.ApplySpeedBoost(15f, 1f);
        }
        else
        {
            Debug.Log("🐢 방어 실패! 속도 감소!");
            // 실패 시 속도 대폭 감소 (거의 멈춤)
            carController.ApplySpeedBoost(-9f, 2.0f);
        }
    }

    IEnumerator ShowGrassField()
    {
        if (grassEffectUI)
        {
            grassEffectUI.SetActive(true);
            yield return new WaitForSeconds(3f);
            grassEffectUI.SetActive(false);
        }
    }
}