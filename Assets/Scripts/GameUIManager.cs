using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class GameUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text timerText;    // 화면 상단: 10초 카운트다운용
    public Text lapText;      // 화면 좌측: 현재 바퀴 수 표시용 (1/3 Laps)
    public Text resultText;   // 화면 중앙: 게임 종료 결과 표시용 (평소엔 비활성)

    void Start()
    {
        // 시작할 때 결과창은 꺼둡니다.
        if (resultText != null) resultText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (LapManager.instance == null) return;

        // 1. 타이머 업데이트 (1등 골인 시에만 작동)
        UpdateTimer();

        // 2. 바퀴 수 업데이트
        UpdateLapInfo();

        // 3. 게임 종료 판정 시 결과창 출력
        if (LapManager.instance.isGameOver)
        {
            ShowFinalResult();
        }
    }

    void UpdateTimer()
    {
        // 누군가 골인했고 아직 게임이 완전히 끝나지 않았을 때
        if (!string.IsNullOrEmpty(LapManager.instance.winnerNetId) && !LapManager.instance.isGameOver)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = $"LIMIT: {LapManager.instance.remainingTime:F1}s";
            timerText.color = Color.red;
        }
        else
        {
            timerText.gameObject.SetActive(false);
        }
    }

    void UpdateLapInfo()
    {
        if (NetworkClient.localPlayer != null)
        {
            var lp = NetworkClient.localPlayer.GetComponent<PlayerLapController>();
            if (lp != null)
            {
                lapText.text = $"LAPS: {lp.currentLap} / {LapManager.instance.totalLaps}";
            }
        }
    }

    void ShowFinalResult()
    {
        if (resultText == null || resultText.gameObject.activeSelf) return;

        resultText.gameObject.SetActive(true);
        var lp = NetworkClient.localPlayer.GetComponent<PlayerLapController>();
        string myId = NetworkClient.localPlayer.netId.ToString();

        // 결과 메시지 분기 (승리 / 완주 / 리타이어)
        if (myId == LapManager.instance.winnerNetId)
        {
            resultText.text = "YOU WIN!";
            resultText.color = Color.yellow;
        }
        else if (lp.hasFinished)
        {
            resultText.text = "GOAL IN!";
            resultText.color = Color.green;
        }
        else
        {
            resultText.text = "RETIRED\n(TIME OVER)";
            resultText.color = Color.gray;
        }
    }
}