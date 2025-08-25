using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private TextMeshProUGUI scoretxtUI;
    private NetworkVariable<int> globalScore = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        globalScore.OnValueChanged += OnScoreChanged;
    }

    private void OnScoreChanged(int prevValue, int newValue)
    {
        scoretxtUI.text = newValue.ToString();
    }

    public void AddScore()
    {
        if (!IsServer)
            return;

        globalScore.Value++;
    }
}
