using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class CharacterData
{
    public int character_id;
    public string name;
    public int price;
    public float stat_speed;
    public string image_url;
}

[System.Serializable]
public class CharacterList
{
    public CharacterData[] characters;
}

[System.Serializable]
public class InventoryData
{
    public int inventory_id;
    public int user_id;
    public int character_id;
    public string purchased_at;
}

[System.Serializable]
public class InventoryList
{
    public InventoryData[] inventory;
}

[System.Serializable]
public class UserData
{
    public int user_id;
    public string nickname;
    public string password;
    public int seed_money;
    public int current_character_id;
    public float sound_volume;
}

public class ShopManager : MonoBehaviour
{
    public TMP_Text currencyText;
    public Image currentSkinImage;
    public Transform shopItemContainer;
    public GameObject shopItemPrefab;

    private string nickname="racer_01"; // 테스트용 닉네임
    private List<CharacterData> allCharacters = new List<CharacterData>();
    private List<InventoryData> userInventory = new List<InventoryData>();
    private UserData userData;

    void Start()
    {
        // 인스펙터 오류로 버튼이 연결 안 될 경우를 대비해 코드로 직접 찾아서 연결
        GameObject btnObj = GameObject.Find("GoToMainButton"); // 버튼 이름을 "GoToShopButton"으로 해주세요
        if (btnObj != null)
        {
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnClickMain);
                Debug.Log("ShopManager: GoToMainButton 연결 성공!");
            }
        }

        StartCoroutine(LoadShopData());
    }
    
    public void OnClickMain()
    {
        SceneManager.LoadScene("Main");
    }
    public IEnumerator LoadShopData()
    {
        yield return StartCoroutine(GetCharacters());
        yield return StartCoroutine(GetUserData());
        yield return StartCoroutine(GetUserInventory());
        UpdateUI();
    }

    IEnumerator GetCharacters()
    {
        UnityWebRequest request = UnityWebRequest.Get("http://localhost:3000/characters");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            CharacterList charList = JsonUtility.FromJson<CharacterList>("{\"characters\":" + json + "}");
            allCharacters.AddRange(charList.characters);
        }
        else
        {
            Debug.LogError("캐릭터 조회 실패: " + request.error);
        }
    }

    IEnumerator GetUserData()
    {
        UnityWebRequest request = UnityWebRequest.Get("http://localhost:3000/users/" + nickname);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            userData = JsonUtility.FromJson<UserData>(json);
        }
        else
        {
            Debug.LogError("유저 조회 실패: " + request.error);
        }
    }

    IEnumerator GetUserInventory()
    {
        UnityWebRequest request = UnityWebRequest.Get("http://localhost:3000/user_inventory/" + nickname);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            InventoryList invList = JsonUtility.FromJson<InventoryList>("{\"inventory\":" + json + "}");
            userInventory.AddRange(invList.inventory);
        }
        else
        {
            Debug.LogError("인벤토리 조회 실패: " + request.error);
        }
    }

    void UpdateUI()
    {
        if (userData != null)
            currencyText.text = "재화: " + userData.seed_money;

        // 현재 스킨: 간단히 ID 표시, 실제로는 이미지 로드
        // currentSkinImage.sprite = ...

        foreach (Transform child in shopItemContainer)
            Destroy(child.gameObject);

        Debug.Log($"[ShopManager] 캐릭터 목록 수: {allCharacters.Count}");
        
        if (shopItemPrefab == null)
        {
            Debug.LogError("[ShopManager] Shop Item Prefab이 연결되지 않았습니다! 인스펙터에서 할당해주세요.");
            return;
        }

        foreach (var charData in allCharacters)
        {
            GameObject item = Instantiate(shopItemPrefab, shopItemContainer);
            
            // ShopItemUI 컴포넌트 가져오기
            ShopItemUI itemUI = item.GetComponent<ShopItemUI>();
            if (itemUI != null)
            {
                Debug.Log($"[ShopManager] {charData.name} 아이템 UI 초기화 중...");
                // 이미지 로드 시도
                Sprite sprite = null;
                if (!string.IsNullOrEmpty(charData.image_url))
                {
                    sprite = Resources.Load<Sprite>(charData.image_url.Replace(".png", ""));
                }

                // UI 초기화
                itemUI.Initialize(
                    charData.name, 
                    charData.price, 
                    sprite, 
                    () => BuyCharacter(charData.character_id)
                );
            }
            else
            {
                Debug.LogError($"[ShopManager] 생성된 프리팹({item.name})에 ShopItemUI 컴포넌트를 찾을 수 없습니다. 프리팹의 **최상위**에 스크립트가 붙어있는지 확인해주세요.");
            }
        }
    }

    void BuyCharacter(int charId)
    {
        StartCoroutine(Purchase(charId));
    }

    IEnumerator Purchase(int charId)
    {
        string json = "{\"nickname\":\"" + nickname + "\", \"character_id\":" + charId + "}";
        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/purchase", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("구매 성공");
            StartCoroutine(LoadShopData()); // 리프레시
        }
        else
        {
            Debug.LogError("구매 실패: " + request.error);
        }
    }
}