using UnityEngine;

public class AutoScroll : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;

    void Update()
    {
        // Move a câmara continuamente para a direita no eixo X
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
    }
}