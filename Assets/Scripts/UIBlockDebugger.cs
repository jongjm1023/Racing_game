using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIBlockDebugger : MonoBehaviour
{
    void Update()
    {
        // 마우스 왼쪽 버튼 클릭 시
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null) return;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                // 가장 위에 있는(클릭을 가로채는) UI 요소 이름 출력
                Debug.Log($"[Click Debug] 🖱️ 클릭된 UI: <color=yellow>{results[0].gameObject.name}</color>", results[0].gameObject);
                
                // 그 아래에 깔린 요소들도 확인하고 싶다면 아래 주석 해제
                /*
                foreach(var result in results)
                {
                     Debug.Log($"   -> (아래에 깔림) {result.gameObject.name}");
                }
                */
            }
            else
            {
                Debug.Log("[Click Debug] UI가 감지되지 않음 (허공 클릭)");
            }
        }
    }
}
