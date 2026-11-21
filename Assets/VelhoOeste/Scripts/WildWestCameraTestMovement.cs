using UnityEngine;

public class CameraTestMovement : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float distance = 4f;
    private float startX;
    private int direction = 1;

    void Start()
    {
        startX = transform.position.x;
    }

    void Update()
    {
        // Move a câmara automaticamente para a esquerda e para a direita
        transform.Translate(Vector3.right * speed * Time.deltaTime * direction);

        // Inverte a direção quando atingir o limite da distância
        if (transform.position.x > startX + distance)
        {
            direction = -1;
        }
        else if (transform.position.x < startX - distance)
        {
            direction = 1;
        }
    }
}