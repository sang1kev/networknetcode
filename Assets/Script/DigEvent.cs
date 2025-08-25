using UnityEngine;

public class DigEvent : MonoBehaviour
{
    private NetworkTilemap tilemap;
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private Transform[] hitPoints;

    void Awake()
    {
        tilemap = FindFirstObjectByType<NetworkTilemap>();
    }

    public void OnDig()
    {
        for (int i = 0; i < hitPoints.Length; i++)
        {
            Collider2D coll = Physics2D.OverlapCircle(hitPoints[i].position, 0.1f, tileLayer);

            if (coll != null)
            { 
                tilemap.RemoveTile(hitPoints[i].position);

                break;
            }
        }
    }
}
