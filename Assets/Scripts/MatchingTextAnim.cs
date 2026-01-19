using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro를 쓴다면 필수

public class MatchingTextAnim : MonoBehaviour
{
    [Header("설정")]
    public TextMeshProUGUI targetText; // 텍스트 컴포넌트 연결
    public float speed = 0.5f; // 점이 바뀌는 속도

    private string originalText; // "매칭중" 원본 텍스트 저장용

    void Start()
    {
        // 스크립트가 시작될 때 현재 텍스트("매칭중")를 저장해둠
        if (targetText == null) targetText = GetComponent<TextMeshProUGUI>();
        originalText = targetText.text;

        // 애니메이션 시작
        StartCoroutine(AnimateDots());
    }

    IEnumerator AnimateDots()
    {
        WaitForSeconds wait = new WaitForSeconds(speed);

        while (true)
        {
            targetText.text = originalText + ".";
            yield return wait;

            targetText.text = originalText + "..";
            yield return wait;

            targetText.text = originalText + "...";
            yield return wait;

            // 다시 . 으로 돌아감 (원한다면 빈 텍스트 단계를 추가해도 됨)
        }
    }

    // 오브젝트가 꺼지면 코루틴도 멈추도록 설정 (안전장치)
    void OnDisable()
    {
        StopAllCoroutines();
    }
}