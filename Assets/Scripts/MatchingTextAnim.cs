using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro를 쓴다면 필수

public class MatchingTextAnim : MonoBehaviour
{
    [Header("설정")]
    public TextMeshProUGUI targetText; // 텍스트 컴포넌트 연결
    public float speed = 0.5f; // 점이 바뀌는 속도

    private string originalText; // "매칭중" 원본 텍스트 저장용

    void Awake()
    {
        if (targetText == null) targetText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        // 텍스트가 아직 저장 안 됐다면 저장
        if (string.IsNullOrEmpty(originalText) && targetText != null)
        {
            originalText = targetText.text;
        }

        // 애니메이션 시작
        StartCoroutine(AnimateDots());
    }

    IEnumerator AnimateDots()
    {
        WaitForSeconds wait = new WaitForSeconds(speed);

        while (true)
        {
            // 아직 originalText가 없을 수도 있음 (방어 코드)
            if (string.IsNullOrEmpty(originalText)) 
            {
                 yield return null; 
                 continue;
            }

            targetText.text = originalText + ".";
            yield return wait;

            targetText.text = originalText + "..";
            yield return wait;

            targetText.text = originalText + "...";
            yield return wait;
        }
    }

    // 오브젝트가 꺼지면 코루틴도 멈추고 텍스트 원상복구
    void OnDisable()
    {
        StopAllCoroutines();
        if (targetText != null && !string.IsNullOrEmpty(originalText))
        {
            targetText.text = originalText;
        }
    }
}