using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayerCtrl : NetworkBehaviour
{
    private NetworkVariable<int> currentAnimState = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private GameObject[] animObjs;

    private Rigidbody2D rb;

    private Vector3 moveInput;

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float jumpPower = 7f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        rb = GetComponent<Rigidbody2D>();
        currentAnimState.OnValueChanged += UpdateAnim;

        if(!IsServer)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (!IsOwner)
        {
            GetComponent<PlayerInput>().enabled = false;
        }
        else
        {
            CamFollow camFollow = Camera.main.GetComponent<CamFollow>();

            if (camFollow != null)
            {
                camFollow.target = transform;
            }
        }
    }


    void Update()
    {
        if (IsOwner)
            MovementServerRpc(moveInput);
    }

    [ServerRpc]
    private void MovementServerRpc(Vector2 moveInput)
    {
        if (currentAnimState.Value == 2)
            return;

        if (moveInput.x == 0)
        {
            currentAnimState.Value = 0;
        }
        else if (moveInput.x != 0)
        {
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

            int dirX = moveInput.x > 0 ? -1 : 1;
            transform.localScale = new Vector3(dirX, 1, 1);
            
            currentAnimState.Value = 1;
        }
    }

    void OnMove(InputValue val)
    {
        moveInput = val.Get<Vector2>();
    }

    void OnAttack()
    {
        if (IsOwner)
        {
            if (currentAnimState.Value != 2)
                AttackServerRpc();
        }
    }

    [ServerRpc]
    private void AttackServerRpc()
    {
        StartCoroutine(Attack());
    }

    IEnumerator Attack()
    {
        currentAnimState.Value = 2;

        yield return new WaitForSeconds(1f);

        currentAnimState.Value = 0;
    }

    void OnJump()
    {
        if (IsOwner)
            JumpServerRpc();
    }

    [ServerRpc]
    private void JumpServerRpc()
    {
        rb.AddForceY(jumpPower, ForceMode2D.Impulse);
    }

    private void UpdateAnim(int prevIndex, int newIndex)
    {
        for (int i = 0; i < animObjs.Length; i++)
            animObjs[i].SetActive(i == newIndex);
    }
}
