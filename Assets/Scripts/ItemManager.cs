using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class ItemManager : NetworkBehaviour
{
    [Header("참조")]
    public CarController2D carController;
    public CarController2D enemyCarController;

    // 아이템 저장소
    public Queue<ItemType> itemQueue = new Queue<ItemType>();

    // UI 변수
    private Image slot1Image;
    private Image slot2Image;
    private GameObject grassEffectUI;
    private GameObject shieldEffectObj;

    // 햄찌 UI
    private GameObject qtePanel;
    private RectTransform qteCursor;

    // =========================================================
    // [수정] 여기가 중요! 빠진 변수를 다시 넣었습니다.
    // =========================================================
    [Header("미니게임 상태")]
    public bool isQteActive = false;
    private float qteCursorPos = 0f;
    private float qteDirection = 1f;
    private float qteTimer = 0f; // <--- 아까 이게 없어서 에러 났던 겁니다!

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

        // X키 입력 로직
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (isQteActive) return; // 미니게임 중엔 사용 불가
            if (itemQueue.Count == 0) return; // 아이템 없으면 사용 불가

            UseItem();
        }

        // 미니게임 업데이트
        if (isQteActive)
        {
            UpdateHamsterQTE();
        }

        // [치트] 1번 키로 햄찌 획득
        if (Input.GetKeyDown(KeyCode.Alpha1)) AddItem(ItemType.HamsterBomb);

        // [긴급 테스트] H키로 바로 실행
        if (Input.GetKeyDown(KeyCode.H)) StartHamsterQTE();
    }

    public void AddItem(ItemType newItem)
    {
        if (itemQueue.Count >= 2) return;
        itemQueue.Enqueue(newItem);
        UpdateItemUI();
    }

    void UseItem()
    {
        if (itemQueue.Count > 0)
        {
            ItemType usedItem = itemQueue.Dequeue();
            ExecuteItemLogic(usedItem);
            UpdateItemUI();
        }
    }

    void UpdateItemUI()
    {
        if (slot1Image == null || slot2Image == null) return;
        ItemType[] items = itemQueue.ToArray();

        slot1Image.enabled = items.Length >= 1;
        if (items.Length >= 1) slot1Image.sprite = inputItemSprites[(int)items[0] - 1];

        slot2Image.enabled = items.Length >= 2;
        if (items.Length >= 2) slot2Image.sprite = inputItemSprites[(int)items[1] - 1];
    }

    void ExecuteItemLogic(ItemType type)
    {
        switch (type)
        {
            case ItemType.HamsterBomb: StartHamsterQTE(); break;
            case ItemType.DashBoom: carController.ApplySpeedBoost(15f, 2f); break;
            case ItemType.Shield: carController.ActivateShield(3f); break;
            case ItemType.GrassField: StartCoroutine(ShowGrassField()); break;
        }
    }

    // ==========================================
    //  햄찌 미니게임 (수정된 버전)
    // ==========================================
    void StartHamsterQTE()
    {
        if (qtePanel == null) return;

        isQteActive = true;

        // 패널과 자식들 켜기
        qtePanel.SetActive(true);
        foreach (Transform child in qtePanel.transform) child.gameObject.SetActive(true);

        // 맨 앞으로 가져오고 위치 초기화
        qtePanel.transform.SetAsLastSibling();
        RectTransform rect = qtePanel.GetComponent<RectTransform>();
        if (rect != null) rect.anchoredPosition = Vector2.zero;

        // 변수 초기화
        qteTimer = 3.0f; // 이제 에러 안 날 겁니다!
        qteCursorPos = 0f;
        qteDirection = 1f;

        Debug.Log("🐹 햄찌 미니게임 시작!");
    }

    void UpdateHamsterQTE()
    {
        qteCursorPos += Time.deltaTime * 2.0f * qteDirection;
        if (qteCursorPos >= 1f) { qteCursorPos = 1f; qteDirection = -1f; }
        if (qteCursorPos <= 0f) { qteCursorPos = 0f; qteDirection = 1f; }

        if (qteCursor != null)
        {
            qteCursor.anchoredPosition = new Vector2((qteCursorPos - 0.5f) * 300f, 0);
        }

        // 타이머 감소
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
            Debug.Log("🎉 성공! 부스트 발동!");
            // 원래 속도 + 15 (엄청 빨라짐)
            carController.ApplySpeedBoost(15f, 1f);
        }
        else
        {
            Debug.Log("🐢 실패! 속도 감소!");

            // [핵심 변경] 스턴 함수 삭제! -> 대신 속도를 깎아버림
            // 기본 속도가 10이라면 -9를 해서 속도 1로 만듦 (거의 멈춤)
            carController.ApplySpeedBoost(-9f, 2.0f);
        }
    }

    IEnumerator ShowGrassField()
    {
        if (grassEffectUI) { grassEffectUI.SetActive(true); yield return new WaitForSeconds(3f); grassEffectUI.SetActive(false); }
    }
}