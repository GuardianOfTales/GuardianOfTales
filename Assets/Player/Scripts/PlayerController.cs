using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimentação")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Verificação de Chão")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Ataque e Magia")]
    public Transform firePoint;
    public GameObject spell1Prefab; // Prefab do Charger2 (Spell_1)
    public GameObject spell2Prefab; // Prefab do Charger1 (Spell_2)

    // Componentes Físicos e Internos
    private Rigidbody2D rb;
    private Animator anim;

    private float moveInput;
    private bool isFacingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. Ler entrada do teclado (A / D ou Setas)
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2. Verificar se está encostado no chão
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        }

        // 3. Pulo (Espaço)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 4. Teclas de Ataque (J = Magia 1 / K = Magia 2)
        if (Input.GetKeyDown(KeyCode.J))
        {
            CastSpell1();
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            CastSpell2();
        }

        // 5. Virar o personagem e o FirePoint para a esquerda/direita
        Flip();

        // 6. Atualizar animações
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        // Aplica a velocidade de movimento
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void CastSpell1()
    {
        if (anim != null) anim.SetTrigger("Attack1"); // Toca Playe_atck1

        if (spell1Prefab != null && firePoint != null)
        {
            // Instancia a Magia 1 na ponta do cajado
            GameObject spell = Instantiate(spell1Prefab, firePoint.position, Quaternion.identity);
            float dir = isFacingRight ? 1f : -1f;
            Projectile proj = spell.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.SetupDirection(dir);
            }
        }
    }

    void CastSpell2()
    {
        if (anim != null) anim.SetTrigger("Attack2"); // Toca Playe_atck2

        if (spell2Prefab != null && firePoint != null)
        {
            // Instancia a Magia 2 na ponta do cajado
            GameObject spell = Instantiate(spell2Prefab, firePoint.position, Quaternion.identity);
            float dir = isFacingRight ? 1f : -1f;
            Projectile proj = spell.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.SetupDirection(dir);
            }
        }
    }

    void UpdateAnimations()
    {
        if (anim != null)
        {
            anim.SetBool("isMoving", Mathf.Abs(moveInput) > 0.1f);
            anim.SetBool("isGrounded", isGrounded);
        }
    }

    void Flip()
    {
        if (moveInput > 0 && !isFacingRight)
        {
            isFacingRight = true;
            transform.eulerAngles = new Vector3(0, 0, 0);
        }
        else if (moveInput < 0 && isFacingRight)
        {
            isFacingRight = false;
            transform.eulerAngles = new Vector3(0, 180, 0);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }
    }
}
