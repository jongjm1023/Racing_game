using UnityEngine;
using Mirror;

public class PlayerLapController : NetworkBehaviour
{
    [SyncVar] public int currentLap = 1;
    public bool hasFinished = false;

    // 체크포인트(Square Sprite)에 OnTriggerEnter2D 설정
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isServer) return; // 서버에서만 판정

        if (collision.CompareTag("FinishLine"))
        {
            if (currentLap < LapManager.instance.totalLaps)
            {
                currentLap++;
                Debug.Log($"{netId} 선수의 현재 바퀴: {currentLap}/{LapManager.instance.totalLaps}");
            }
            else if (!hasFinished)
            {
                // n/n 상태에서 통과 시 골인
                hasFinished = true;
                LapManager.instance.OnPlayerFinished(netId.ToString());
                Debug.Log($"{netId} 골인!");
            }
        }
    }
}