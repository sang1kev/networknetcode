using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkScoreManager : NetworkBehaviour
{
    public static NetworkScoreManager Instance;

    [SerializeField] private TextMeshProUGUI scoreUI;

    private NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        score.OnValueChanged += OnScoreChanged;

        scoreUI.text = score.Value.ToString();
    }

    private void OnScoreChanged(int prevValue, int newValue)
    {
        scoreUI.text = newValue.ToString();
    }

    public void AddScore()
    {
        if (!IsServer)
            return;

        score.Value++;
    }
}
