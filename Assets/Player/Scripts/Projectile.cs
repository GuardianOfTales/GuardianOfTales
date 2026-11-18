using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f; // Destrói a magia se não atingir nada após 3s
    public int damage = 1;

    private float direction = 1f;

    public void SetupDirection(float dir)
    {
        direction = dir;
        // Inverte o sprite da magia se estiver atirando para a esquerda
        if (dir < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    void Start()
    {
        Destroy(gameObject, lifeTime); // Autodestruição
    }

    void Update()
    {
        // Move a magia para a frente
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Se colidir com um Inimigo ou Chão/Parede
        if (collision.CompareTag("Enemy"))
        {
            // Aplica dano no inimigo se tiver o script
            Destroy(gameObject); // Destrói a magia ao impactar
        }
        else if (collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
