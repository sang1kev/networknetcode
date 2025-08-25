using StarterAssets;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevel;

public class CharMove : NetworkBehaviour
{
    [SerializeField] private CharacterController cc;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private StarterAssetsInputs starterAsset;
    [SerializeField] private ThirdPersonController thridCtrl;
    [SerializeField] private Transform playerRoot;

    [SerializeField] private GameObject bombPrefab;

    void Awake()
    {
        cc.enabled = false;
        playerInput.enabled = false;
        starterAsset.enabled = false;
        thridCtrl.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            cc.enabled = true;
            playerInput.enabled = true;
            starterAsset.enabled = true;
            thridCtrl.enabled = true;

            var cinemachine = GameObject.Find("PlayerFollowCamera").GetComponent<CinemachineCamera>();
            cinemachine.Target.TrackingTarget = playerRoot;
        }
    }

    void Update()
    {
        if (!IsOwner)
            return;
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            AddScoreServerRpc();
        }
    }

    void OnAttack()
    {
        ThrowBombServerRpc();
    }

    [ServerRpc]
    private void ThrowBombServerRpc()
    {
        Instantiate(bombPrefab, transform.position, Quaternion.identity);
    }

    [ServerRpc]
    private void AddScoreServerRpc()
    {
        ScoreManager.Instance.AddScore();
    }
}
