using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkBomb : NetworkBehaviour
{
    private float timer = 0f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            return;
        }

        base.OnNetworkSpawn();
    }

    void Update()
    {
        transform.Translate(Vector3.up * 10f *  Time.deltaTime);

        timer += Time.deltaTime;

        if (timer>= 3f)
        {
            timer = 0f;
            ActiveBombServerRpc();
        }
    }

    [ServerRpc]
    private void ActiveBombServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
    }
}
