using UnityEngine;

public class FaceTowardPlayer : MonoBehaviour
{
    private Player player;
    void Start()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
    }

    void Update()
    {
        if (player != null)
            transform.forward = player.transform.forward;
    }
}