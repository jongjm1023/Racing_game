using System.Collections;
using UnityEngine;

public class ItemBox : MonoBehaviour
{
    [Header("설정")]
    public float respawnTime = 3.0f; // 먹고나서 다시 생기는 시간
    public GameObject visualModel;   // 큐브(눈에 보이는 상자 모델)

    private Collider2D col;          // 충돌체
    private Renderer rend;           // 렌더러(보여주는 것)

    void Start()
    {
        col = GetComponent<Collider2D>();
        // visualModel이 따로 있다면 그것을 끄고, 아니면 자기 자신 렌더러를 끔
        if (visualModel != null) rend = visualModel.GetComponent<Renderer>();
        else rend = GetComponent<Renderer>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 플레이어 태그 확인
        if (other.CompareTag("Player"))
        {
            // 2. 플레이어의 ItemManager 찾기
            ItemManager manager = other.GetComponent<ItemManager>();

            if (manager != null)
            {
                // 3. 랜덤 아이템 뽑기 (1~4번 중 랜덤)
                // (ItemType Enum 순서에 따라 숫자는 달라질 수 있음)
                // 여기선 1:Dash, 2:Shield, 3:Grass, 4:Hamster 라고 가정
                int randomIndex = Random.Range(1, 5);
                ItemType randomItem = (ItemType)randomIndex;

                // 4. 아이템 지급
                manager.AddItem(randomItem);

                // 5. 상자 숨기기 (삭제하지 않고 안 보이게만 함)
                StartCoroutine(RespawnRoutine());
            }
        }
    }

    IEnumerator RespawnRoutine()
    {
        // 안 보이게 하고 충돌 끄기
        if (rend) rend.enabled = false;
        if (col) col.enabled = false;

        yield return new WaitForSeconds(respawnTime);

        // 다시 보이게 하고 충돌 켜기
        if (rend) rend.enabled = true;
        if (col) col.enabled = true;
    }
}

// ItemBox.cs 의 맨 아랫부분 혹은 Enum 정의된 곳

public enum ItemType
{
    None = 0,
    HamsterBomb = 1, // 1. 햄찌 (리듬게임)
    GrassField = 2,  // 2. 풀밭
    DashBoom = 3,    // 3. 대시 (순서 변경됨!)
    Shield = 4       // 4. 방어막 (순서 변경됨!)
}