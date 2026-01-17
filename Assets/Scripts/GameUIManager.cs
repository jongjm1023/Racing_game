using UnityEngine;
using UnityEngine.UI; // 기본 Text를 위해 필수
using Mirror;

public class GameUIManager : MonoBehaviour
{
    [Header("UI Elements (기본 Text 객체를 드래그하세요)")]
    public Text timerText;
    public Text lapText;
    public Text resultText;

    void Update()
    {
        if (LapManager.instance == null)
        {
            LapManager.instance = GameObject.FindObjectOfType<LapManager>();
            return;
        }

        UpdateLapInfo();
        UpdateTimer();

        if (LapManager.instance.isGameOver)
        {
            ShowFinalResult();
        }
    }

    void UpdateLapInfo()
    {
        if (NetworkClient.localPlayer != null)
        {
            var lp = NetworkClient.localPlayer.GetComponent<PlayerLapController>();
            if (lp != null)
            {
                float t = lp.GetRaceTime();
                int min = (int)(t / 60);
                int sec = (int)(t % 60);
                int msec = (int)((t * 100) % 100);

                // 1. currentLap이 1부터 시작하는지 확인
                // 2. \n을 써서 바퀴 수 아래에 시간 표시
                lapText.text = string.Format("LAPS: {0} / {1}\nTIME: {2:00}:{3:00}.{4:02}",
                    lp.currentLap,
                    LapManager.instance != null ? LapManager.instance.totalLaps : 3,
                    min, sec, msec);
            }
        }
        else
        {
            // 플레이어 소환 전 기본 표시
            lapText.text = "LAPS: 1 / 3\nTIME: 00:00.00";
        }
    }
    void UpdateTimer()
    {
        if (!string.IsNullOrEmpty(LapManager.instance.winnerNetId) && !LapManager.instance.isGameOver)
        {
            if (!timerText.gameObject.activeSelf) timerText.gameObject.SetActive(true);
            timerText.text = "LIMIT: " + LapManager.instance.remainingTime.ToString("F1") + "s";
        }
        else
        {
            if (timerText.gameObject.activeSelf) timerText.gameObject.SetActive(false);
        }
    }

    void ShowFinalResult()
    {
        if (resultText == null || resultText.gameObject.activeSelf) return;
        resultText.gameObject.SetActive(true);

        var lp = NetworkClient.localPlayer?.GetComponent<PlayerLapController>();
        string myId = NetworkClient.localPlayer.netId.ToString();

        if (myId == LapManager.instance.winnerNetId)
        {
            resultText.text = "YOU WIN!";
            resultText.color = Color.yellow;
        }
        else if (lp != null && lp.hasFinished)
        {
            resultText.text = "GOAL IN!";
            resultText.color = Color.green;
        }
        else
        {
            resultText.text = "RETIRED\n(TIME OVER)";
            resultText.color = Color.red;
        }
    }
}