using UnityEngine;
using Mirror;
using System.Collections;

public class LapManager : NetworkBehaviour
{
    public static LapManager instance;
    public int totalLaps = 3;

    [SyncVar] public bool isGameOver = false;
    [SyncVar] public float remainingTime = 10f;
    [SyncVar] public string winnerNetId = "";

    private void Awake() { instance = this; }

    [Server]
    public void OnPlayerFinished(string netId)
    {
        if (string.IsNullOrEmpty(winnerNetId))
        {
            winnerNetId = netId;
            StartCoroutine(FinishCountdown());
        }
    }

    IEnumerator FinishCountdown()
    {
        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }
        EndGame();
    }

    void EndGame()
    {
        isGameOver = true;
        Debug.Log("게임 종료! 승자: " + winnerNetId);
        // 여기에 결과창 출력 로직 추가
    }
}