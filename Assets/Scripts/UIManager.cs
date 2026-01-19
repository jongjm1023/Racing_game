using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // 어디서든 UIManager.Instance로 부를 수 있게 함 (싱글톤 패턴)
    public static UIManager Instance;

    void Awake()
    {
        Instance = this;
    }

    [Header("연결할 UI들")]
    public Image slot1;
    public Image slot2;
    public GameObject grassPanel;
    public GameObject hamsterPanel;
    public RectTransform hamsterCursor;
    public RectTransform successZone;
}