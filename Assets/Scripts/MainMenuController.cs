using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuController : MonoBehaviour
{
    public UnityEngine.UI.Image currentSkinImage; // 인스펙터에서 연결 필요
    
    private string nickname = "racer_01"; // 테스트용 닉네임 (나중에 로그인 연동 필요)
    private UserData userData;
    private System.Collections.Generic.List<CharacterData> allCharacters = new System.Collections.Generic.List<CharacterData>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (currentSkinImage == null)
        {
            Debug.LogError("MainMenuController: Current Skin Image가 연결되지 않았습니다.");
        }
        else
        {
            StartCoroutine(LoadData());
        }
    }

    // 데이터 로드 코루틴
    System.Collections.IEnumerator LoadData()
    {
        // 1. 캐릭터 목록 조회
        UnityEngine.Networking.UnityWebRequest charRequest = UnityEngine.Networking.UnityWebRequest.Get("http://localhost:3000/characters");
        yield return charRequest.SendWebRequest();

        if (charRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string json = charRequest.downloadHandler.text;
            CharacterList charList = JsonUtility.FromJson<CharacterList>("{\"characters\":" + json + "}");
            allCharacters = new System.Collections.Generic.List<CharacterData>(charList.characters);
        }
        else
        {
            Debug.LogError("캐릭터 목록 로드 실패: " + charRequest.error);
        }

        // 2. 유저 정보 조회
        UnityEngine.Networking.UnityWebRequest userRequest = UnityEngine.Networking.UnityWebRequest.Get("http://localhost:3000/users/" + nickname);
        yield return userRequest.SendWebRequest();

        if (userRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            userData = JsonUtility.FromJson<UserData>(userRequest.downloadHandler.text);
            UpdateCurrentSkinUI();
        }
        else
        {
             Debug.LogError("유저 정보 로드 실패: " + userRequest.error);
        }
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
    // 왼쪽 현재 스킨 이미지 업데이트
    void UpdateCurrentSkinUI()
    {
        if (userData == null || currentSkinImage == null) return;

        Debug.Log($"[MainMenu] 현재 장착 ID: {userData.current_character_id}");

        // 현재 장착된 캐릭터 ID에 맞는 스프라이트 찾기
        CharacterData equippedChar = allCharacters.Find(c => c.character_id == userData.current_character_id);
        if (equippedChar != null && !string.IsNullOrEmpty(equippedChar.image_url))
        {
            string baseName = equippedChar.image_url.Replace(".png", "");
            string targetName = baseName + "_1"; // 메인메뉴도 _1 버전 사용
            Sprite sprite = Resources.Load<Sprite>(targetName);
            
            if (sprite != null)
            {
                currentSkinImage.sprite = sprite;
                currentSkinImage.color = Color.white; 
            }
            else
            {
                Debug.LogError($"[MainMenu] 스프라이트를 찾을 수 없습니다: {targetName}");
            }
        }
    }

}
