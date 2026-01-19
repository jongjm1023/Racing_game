using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro를 쓴다면 필수

public class MatchingTextAnim : MonoBehaviour
{
    public TextMeshProUGUI statusText; // 일반 Text라면 'Text'로 변경
    private string baseText = "매칭중";

    void Start()
    {
        StartCoroutine(AnimateDots());
    }

    IEnumerator AnimateDots()
    {
        while (true)
        {
            statusText.text = baseText + ".";
            yield return new WaitForSeconds(0.5f); // 0.5초 대기

            statusText.text = baseText + "..";
            yield return new WaitForSeconds(0.5f);

            statusText.text = baseText + "...";
            yield return new WaitForSeconds(0.5f);
        }
    }
}