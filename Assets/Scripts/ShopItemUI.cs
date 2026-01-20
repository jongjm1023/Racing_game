using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Image itemImage;
    public Button buyButton;

    // 아이템 데이터를 받아 UI를 설정하는 함수
    public void Initialize(string name, int price, Sprite sprite, bool isOwned, bool isEquipped, UnityAction onBuy)
    {
        if (nameText == null) Debug.LogError($"[ShopItemUI] '{name}' 아이템의 NameText가 연결되지 않았습니다! 프리팹을 확인하세요.");
        else nameText.text = name;
        
        if (priceText == null) 
        {
            Debug.LogError($"[ShopItemUI] '{name}' 아이템의 PriceText가 연결되지 않았습니다! 프리팹을 확인하세요.");
        }
        else 
        {
            if (isEquipped) 
            {
                priceText.text = "<color=blue>장착중</color>"; // 강조
            }
            else if (isOwned) 
            {
                priceText.text = "장착하기";
            }
            else 
            {
                priceText.text = $"{price:N0} 해씨";
            }
        }

        if (itemImage == null) Debug.LogError($"[ShopItemUI] '{name}' 아이템의 ItemImage가 연결되지 않았습니다! 프리팹을 확인하세요.");
        else if (sprite != null) itemImage.sprite = sprite;

        // 버튼 리스너 초기화 (중복 방지)
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => Debug.Log($"[ShopItemUI] 버튼 클릭됨: {name}")); // 클릭 확인용 로그
            buyButton.onClick.AddListener(onBuy);
            Debug.Log($"[ShopItemUI] '{name}' 버튼 리스너 연결 완료");
        }

        else
        {
             Debug.LogError($"[ShopItemUI] '{name}' 아이템의 BuyButton이 연결되지 않았습니다! 프리팹을 확인하세요.");
        }

        // [추가] ShopItem 자체를 클릭해도 동작하게 하기
        // 이미지 컴포넌트가 없으면 클릭을 못 받으므로 투명 이미지를 추가합니다.
        Button mainBtn = GetComponent<Button>();
        if (mainBtn == null) mainBtn = gameObject.AddComponent<Button>();
        
        // 클릭 감지용 투명 이미지 확인
        Image raycastImg = GetComponent<Image>();
        if (raycastImg == null)
        {
            raycastImg = gameObject.AddComponent<Image>();
            raycastImg.color = new Color(0,0,0,0); // 투명
            raycastImg.raycastTarget = true;       // 클릭 감지 ON
        }

        mainBtn.onClick.RemoveAllListeners();
        mainBtn.onClick.AddListener(onBuy);
    }
}
