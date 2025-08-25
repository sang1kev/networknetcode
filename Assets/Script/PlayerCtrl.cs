using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCtrl : MonoBehaviour
{
    [SerializeField] private GameObject[] animObjs;

    private Rigidbody2D rb;

    private Vector3 moveInput;

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float jumpPower = 7f;

    private bool isAttack = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();   
    }

    void Update()
    {
        if (isAttack)
            return;

        animObjs[0].SetActive(moveInput.x == 0);
        animObjs[1].SetActive(moveInput.x != 0);


        if (moveInput.x != 0)
        {
            int dirX = moveInput.x > 0 ? -1 : 1;
            transform.localScale = new Vector3(dirX, 1, 1);

            transform.position += moveInput * moveSpeed * Time.deltaTime;   
        }
    }

    void OnMove(InputValue val)
    {
        var moveVal = val.Get<Vector2>();

        moveInput = new Vector3(moveVal.x, 0, 0);
    }

    void OnAttack()
    {
        if (isAttack)
            return;

        StartCoroutine(Attack());
    }

    IEnumerator Attack()
    {
        isAttack = true;
        animObjs[0].SetActive(false);
        animObjs[1].SetActive(false);
        animObjs[2].SetActive(true);

        yield return new WaitForSeconds(1f);

        animObjs[0].SetActive(true);
        animObjs[1].SetActive(false);
        animObjs[2].SetActive(false);
        isAttack = false;
    }

    void OnJump()
    {
        rb.AddForceY(jumpPower, ForceMode2D.Impulse);
    }
}
