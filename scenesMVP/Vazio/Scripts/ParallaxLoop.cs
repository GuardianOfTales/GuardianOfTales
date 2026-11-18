using UnityEngine;

public class ParallaxLoop : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float parallaxEffect = 0.1f;
    [SerializeField] private float width;

    private Vector3 startPosition;
    private Vector3 startCameraPosition;

    private void Start()
    {
        startPosition = transform.position;
        startCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        // Calcula o deslocamento da câmera desde o início
        float distanceX = cameraTransform.position.x - startCameraPosition.x;

        // Aplica o efeito de Parallax
        float parallaxPosition = distanceX * parallaxEffect;

        // Verifica se a camada precisa ser reposicionada
        float loopDistance = cameraTransform.position.x - startPosition.x;

        if (Mathf.Abs(loopDistance) >= width)
        {
            float offset = Mathf.Floor(loopDistance / width) * width;
            startPosition.x += offset;
        }

        // Define a posição final da camada
        transform.position = new Vector3(
            startPosition.x + parallaxPosition,
            startPosition.y,
            startPosition.z
        );
    }
}