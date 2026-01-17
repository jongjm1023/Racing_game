using UnityEngine;
using UnityEngine.Tilemaps; // 이게 없으면 Tilemap 단어를 못 알아듣습니다.

public class PlayerMovement : MonoBehaviour
{
    public float baseSpeed = 5f;
    public Tilemap groundTilemap;

    void Start()
    {
        // [수정 포인트 1] GameObject를 찾고나서 -> 그 안의 Tilemap 컴포넌트를 꺼냄
        GameObject mapObj = GameObject.Find("Tilemap");
        if (mapObj != null)
        {
            groundTilemap = mapObj.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogError("야! Hierarchy창에 'Tilemap'이라는 이름의 오브젝트가 없어!");
        }
    }

    void Update()
    {
        // 타일맵이 연결 안 됐으면 실행 중지 (에러 방지)
        if (groundTilemap == null) return;

        float currentSpeed = baseSpeed;

        Vector3Int cellPosition = groundTilemap.WorldToCell(transform.position);
        TileBase currentTile = groundTilemap.GetTile(cellPosition);

        // 현재 밟은 타일이 내가 만든 RoadTile인지 확인
        if (currentTile is RoadTile)
        {
            // [수정 포인트 2] (RoadTile)을 붙여서 형변환
            RoadTile roadTile = (RoadTile)currentTile;
            currentSpeed *= roadTile.speedMultiplier;
        }

        // 이동 코드 (예시)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(h, v, 0) * currentSpeed * Time.deltaTime);
    }
}