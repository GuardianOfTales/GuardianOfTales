using UnityEngine;

public class GroundLoop : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float width = 38f;

    private void Update()
    {
        if (cameraTransform.position.x >= transform.position.x + width)
        {
            transform.position += Vector3.right * width * 2f;
        }
    }
}