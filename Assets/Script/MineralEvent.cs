using Unity.Netcode;
using UnityEngine;

public class MineralEvent : NetworkBehaviour
{
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Player") && IsOwner)
        {
            AddScoreServerRpc();
        }
    }

    [ServerRpc]
    private void AddScoreServerRpc()
    {
        NetworkScoreManager.Instance.AddScore();
        GetComponent<NetworkObject>().Despawn();
    }
}
