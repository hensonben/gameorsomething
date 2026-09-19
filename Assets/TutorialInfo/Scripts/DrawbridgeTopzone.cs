using UnityEngine;

public class DrawbridgeTopZone : MonoBehaviour
{
    private Drawbridge bridge;

    void Start()
    {
        bridge = GetComponentInParent<Drawbridge>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HeavyPlayer"))
        {
            bridge.TriggerFall();
        }
    }
}