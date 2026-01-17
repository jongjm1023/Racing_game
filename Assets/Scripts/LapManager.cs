using UnityEngine;
using Mirror;
using System.Collections;

public class LapManager : NetworkBehaviour
{
    public static LapManager instance;

    [Header("Game Settings")]
    public int totalLaps = 3;

    [Header("Sync Variables")]
    [SyncVar] public bool isGameOver = false;
    [SyncVar] public float remainingTime = 10f;
    [SyncVar] public string winnerNetId = ""; // 첫 번째로 들어온 사람의 ID 저장

    private void Awake()
    {
        // 싱글톤 설정
        if (instance == null) instance = this;
    }

    // [중요] PlayerLapController에서 호출하는 함수
    [Server]
    public void OnPlayerFinished(string netId)
    {
        // 아직 승자가 없을 때만 실행 (첫 번째 골인자 발생)
        if (string.IsNullOrEmpty(winnerNetId))
        {
            winnerNetId = netId;
            Debug.Log($"Winner detected: {netId}. Starting 10s countdown.");
            StartCoroutine(FinishCountdown());
        }
    }

    // 10초 카운트다운 로직
    private IEnumerator FinishCountdown()
    {
        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(0.1f); // 0.1초씩 깎아서 부드럽게 표시
            remainingTime -= 0.1f;
        }

        remainingTime = 0;
        isGameOver = true;
        Debug.Log("Game Over! Remaining players retired.");
    }

    // LapManager의 EndGame 함수 (기존 코드에 추가)
    void EndGame()
    {
        isGameOver = true;

        // 모든 플레이어를 찾아 멈추게 함
        PlayerLapController[] allPlayers = FindObjectsOfType<PlayerLapController>();
        foreach (var p in allPlayers)
        {
            if (!p.hasFinished)
            {
                // 골인 못한 플레이어들도 조작 중지
                p.SendMessage("RpcStopVehicle", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
    
}