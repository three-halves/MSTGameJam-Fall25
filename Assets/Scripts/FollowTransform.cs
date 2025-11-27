using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    public Transform target;

    public void Update()
    {
        transform.position = target.position;
    }
}