using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class MinimapController : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform minimapRect; // 미니맵 전체 영역 (배경 이미지)
    public GameObject myIconPrefab;   // 내 캐릭터 아이콘 (초록색 점)
    public GameObject enemyIconPrefab; // 적 캐릭터 아이콘 (빨간색 점)
    public Transform iconContainer;   // 아이콘이 생성될 부모 Transform (minimapRect 안)

    [Header("Map Settings")]
    public Tilemap worldTilemap; // 맵의 크기를 측정할 타일맵 (자동으로 찾음)
    
    // 맵의 경계 (World 좌표)
    private float minX, maxX, minY, maxY;
    
    // 플레이어 목록
    private List<CarController2D> allCars = new List<CarController2D>();
    private Dictionary<CarController2D, RectTransform> carIcons = new Dictionary<CarController2D, RectTransform>();

    void Start()
    {
        // UI가 연결되지 않았다면 자동으로 생성 (사용자 편의)
        /*
        if (minimapRect == null)
        {
            GenerateDefaultMinimapUI();
        }
        */
        if (myIconPrefab == null) myIconPrefab = CreateSimpleIconPrefab("MyIcon", Color.blue);
        if (enemyIconPrefab == null) enemyIconPrefab = CreateSimpleIconPrefab("EnemyIcon", Color.red);
        InitializeMapBounds();
        StartCoroutine(FindPlayersRoutine());
    }
    /*
    void GenerateDefaultMinimapUI()
    {
        Debug.Log("[Minimap] UI가 할당되지 않아 자동으로 기본 UI를 생성합니다.");

        // 1. 캔버스 찾기 (없으면 안됨)
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Minimap] 씬에 Canvas가 없습니다! UI를 생성할 수 없습니다.");
            return;
        }

        // 2. 미니맵 패널 생성 (우측 상단)
        GameObject panelObj = new GameObject("MinimapPanel", typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);
        
        minimapRect = panelObj.GetComponent<RectTransform>();
        minimapRect.anchorMin = new Vector2(1, 1); // 우측 상단 고정
        minimapRect.anchorMax = new Vector2(1, 1);
        minimapRect.pivot = new Vector2(1, 1);
        minimapRect.anchoredPosition = new Vector2(-20, -20); // 여백
        minimapRect.sizeDelta = new Vector2(200, 200); // 크기 200x200

        Image bgImage = panelObj.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f); // 반투명 검정

        // 3. 아이콘 컨테이너 (패널과 같음)
        iconContainer = panelObj.transform;

        // 4. 아이콘 프리팹 임시 생성 (메모리 상에만 존재하도록 설정하거나, 리소스가 없으면 코드로 생성)
        
    }
*/
    GameObject CreateSimpleIconPrefab(string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(Image));
        // 프리팹처럼 쓰기 위해 비활성화 해둠 -> Instantiate할 때 활성화됨? 
        // 아니, 여기서는 Instantiate용 원본이 필요하므로..
        // 씬에 숨겨두고 원본으로 씀.
        go.transform.SetParent(this.transform); // MinimapController 자식으로 숨김
        go.SetActive(false); 

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(10, 10); // 점 크기

        Image img = go.GetComponent<Image>();
        img.color = color;
        
        return go;
    }

    void InitializeMapBounds()
    {
        if (worldTilemap == null)
        {
            // "Tilemap"이라는 이름의 오브젝트를 찾거나, 타입으로 찾기
            var found = GameObject.Find("Tilemap");
            if (found != null) worldTilemap = found.GetComponent<Tilemap>();
            else worldTilemap = FindFirstObjectByType<Tilemap>();
        }

        if (worldTilemap != null)
        {
            // 타일맵의 로컬 바운드를 월드 바운드로 변환 (Transform 적용)
            // 보통 타일맵은 (0,0,0)에 있고 스케일이 1이므로 localBounds를 써도 무방하지만,
            // 더 정확히 하려면 CellBounds를 써야 함.
            worldTilemap.CompressBounds(); // 빈 공간 정리
            Bounds bounds = worldTilemap.localBounds;

            minX = bounds.min.x;
            maxX = bounds.max.x;
            minY = bounds.min.y;
            maxY = bounds.max.y;

            Debug.Log($"[Minimap] 맵 경계 설정 완료: X({minX}~{maxX}), Y({minY}~{maxY})");
        }
        else
        {
            Debug.LogError("[Minimap] 타일맵을 찾을 수 없습니다! 맵 경계를 수동으로 설정해주세요.");
            // 임시 기본값
            minX = -50; maxX = 50; minY = -50; maxY = 50;
        }
    }

    // 멀티플레이어라 다른 플레이어가 늦게 생성될 수 있으므로 주기적으로 찾기
    IEnumerator FindPlayersRoutine()
    {
        while (true)
        {
            FindAndCreateIcons();
            yield return new WaitForSeconds(1.0f); // 1초마다 새로운 플레이어 체크
        }
    }

    void FindAndCreateIcons()
    {
        // 씬의 모든 차 찾기
        CarController2D[] cars = FindObjectsByType<CarController2D>(FindObjectsSortMode.None);
        
        foreach (var car in cars)
        {
            if (!carIcons.ContainsKey(car))
            {
                // 새 플레이어 발견! 아이콘 생성
                GameObject prefab = car.isLocalPlayer ? myIconPrefab : enemyIconPrefab;
                
                if (prefab != null && iconContainer != null)
                {
                    GameObject iconObj = Instantiate(prefab, iconContainer);
                    iconObj.SetActive(true); // 프리팹이 비활성화 상태일 수 있으므로 켬
                    RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                    
                    // 앵커를 Bottom-Left(0,0)로 초기화 (Update 로직 단순화 위함)
                    iconRect.anchorMin = Vector2.zero;
                    iconRect.anchorMax = Vector2.zero;
                    iconRect.pivot = new Vector2(0.5f, 0.5f); // 중심점

                    carIcons.Add(car, iconRect);
                }
            }
        }
    }

    void Update()
    {
        // 각 차의 위치를 UI 위치로 변환
        foreach (var kvp in carIcons)
        {
            CarController2D car = kvp.Key;
            RectTransform icon = kvp.Value;

            if (car == null) continue; // 차가 파괴되었으면 패스

            Vector3 carPos = car.transform.position;

            // 0.0 ~ 1.0 사이 값으로 정규화
            float normalizedX = Mathf.InverseLerp(minX, maxX, carPos.x);
            float normalizedY = Mathf.InverseLerp(minY, maxY, carPos.y);

            // 미니맵 UI 크기에 맞게 좌표 매핑
            // 앵커가 BottomLeft(0,0)이라고 가정하고 계산
            float mapWidth = minimapRect.rect.width;
            float mapHeight = minimapRect.rect.height;

            Vector2 uiPos = new Vector2(normalizedX * mapWidth, normalizedY * mapHeight);
            
            icon.anchoredPosition = uiPos;
        }
    }
}
