using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkTransform : NetworkTransform
{
    // "서버만 권한이 있니?" 라고 물으면 "아니오(false), 클라이언트도 움직일 수 있어요" 라고 대답함
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}