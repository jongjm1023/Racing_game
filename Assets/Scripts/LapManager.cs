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

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // 씬이 바뀌어도 파괴되지 않게 하고 싶다면 아래 주석 해제
            // DontDestroyOnLoad(gameObject); 
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 클라이언트가 시작될 때 인스턴스 다시 확인 (중요)
    public override void OnStartClient()
    {
        base.OnStartClient();
        instance = this;
    }

    [Server]
    public void OnPlayerFinished(string netId)
    {
        if (string.IsNullOrEmpty(winnerNetId))
        {
            winnerNetId = netId;
            StartCoroutine(FinishCountdown());
        }
    }

    private IEnumerator FinishCountdown()
    {
        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(0.1f);
            remainingTime -= 0.1f;
        }
        remainingTime = 0;
        isGameOver = true;
        RpcStopAllPlayers();
    }

    [ClientRpc]
    void RpcStopAllPlayers()
    {
        PlayerLapController[] players = FindObjectsOfType<PlayerLapController>();
        foreach (var p in players) p.StopVehicle();
    }
}