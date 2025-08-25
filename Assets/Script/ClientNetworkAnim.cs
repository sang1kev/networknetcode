using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkAnim : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
