using UnityEngine;
using Mirror;

public class PlayerLapController : NetworkBehaviour
{
    [SyncVar] public int currentLap = 1;
    [SyncVar] public bool hasFinished = false;

    // 역주행 방지용 플래그: 중간 지점을 지났는가?
    private bool passedHalfWay = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isServer) return;

        // 1. 중간 지점 통과 체크 (역주행 방지)
        if (collision.CompareTag("HalfWay"))
        {
            passedHalfWay = true;
        }

        // 2. 결승선 통과 체크
        if (collision.CompareTag("FinishLine"))
        {
            // 중간 지점을 찍고 왔을 때만 인정
            if (passedHalfWay)
            {
                ProcessLap();
                passedHalfWay = false; // 다음 바퀴를 위해 초기화
            }
            else
            {
                Debug.Log("역주행 혹은 중간 지점 미통과!");
            }
        }
    }

    [Server]
    void ProcessLap()
    {
        // 에러 방지용 체크
        if (LapManager.instance == null)
        {
            Debug.LogError("LapManager 인스턴스를 찾을 수 없습니다! 씬에 LapManager 오브젝트가 있는지 확인하세요.");
            return;
        }

        if (currentLap < LapManager.instance.totalLaps)
        {
            currentLap++;
            Debug.Log($"바퀴수 증가: {currentLap}");
        }
        else if (!hasFinished)
        {
            hasFinished = true;
            LapManager.instance.OnPlayerFinished(netId.ToString());
            RpcStopVehicle();
        }
    }

    // ClientRpc에서 분리하여 내부에서도 호출 가능하게 수정
    [ClientRpc]
    void RpcStopVehicle()
    {
        StopVehicle();
    }

    public void StopVehicle()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.linearDamping = 10f;
        }
        // 여기에 이동 스크립트 비활성화 추가 (예: GetComponent<CarController>().enabled = false;)
    }
}