using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkTrans : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
