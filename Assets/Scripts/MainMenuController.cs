using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    // --- 스타트 버튼이 부를 함수 ---
    public void OnClickStart() 
    {
        SceneManager.LoadScene("SampleScene");
        // 씬 이동 코드
    }

    // --- 상점 버튼이 부를 함수 ---
    public void OnClickShop()
    {
        SceneManager.LoadScene("SampleScene2");
    }


}
