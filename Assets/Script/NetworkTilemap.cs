using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NetworkTilemap : NetworkBehaviour
{
    [SerializeField] private GameObject[] minerals;

    private Tilemap tilemap;

    private NetworkList<Vector3Int> destroyedTiles = new NetworkList<Vector3Int>();

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        destroyedTiles.OnListChanged += OnTileDestoryed;

        foreach (var tilePos in destroyedTiles)
        {
            tilemap.SetTile(tilePos, null);
        }
    }

    public void RemoveTile(Vector3 hitPos)
    {
        if (!IsServer)
        {
            return;
        }

        Vector3Int cellPos = tilemap.WorldToCell(hitPos);
        
        // Item Drop
        int ranItemDrop = Random.Range(0, 101);
        if (ranItemDrop >= 70)
        {
            int ranIndex = Random.Range(0, minerals.Length);
            //NetworkObject.Instantiate(minerals[ranIndex], cellPos, Quaternion.identity);
            GameObject mineral = Instantiate(minerals[ranIndex], cellPos, Quaternion.identity);
            mineral.GetComponent<NetworkObject>().Spawn();
        }

        if (tilemap.GetTile(cellPos) != null)
        {
            destroyedTiles.Add(cellPos);
        }
    }

    private void OnTileDestoryed(NetworkListEvent<Vector3Int> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<Vector3Int>.EventType.Add)
        {
            tilemap.SetTile(changeEvent.Value, null);
        }
    }
}
