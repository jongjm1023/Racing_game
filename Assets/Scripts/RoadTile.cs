using UnityEngine;
using UnityEngine.Tilemaps;

// 우클릭 -> Create 메뉴에 이 타일을 생성하는 버튼을 추가합니다.
[CreateAssetMenu(fileName = "New Road Tile", menuName = "Tiles/Road Tile")]
public class RoadTile : Tile
{
    // 속도 배율 (1 = 정상 속도, 0.5 = 절반 속도, 도로아닌곳 밟으면 느려지게)
    public float speedMultiplier = 1.0f;
}