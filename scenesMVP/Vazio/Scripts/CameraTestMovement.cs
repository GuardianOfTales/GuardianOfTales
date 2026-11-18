using UnityEngine;

public class CameraTestMovement : MonoBehaviour
{
    [SerializeField] private float speed = 2f;

    private void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
    }
}