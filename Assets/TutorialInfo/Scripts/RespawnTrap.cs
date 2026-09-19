using UnityEngine;

public class RespawnTrap : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        HeavyPlayerController heavy = other.GetComponent<HeavyPlayerController>();
        if (heavy != null)
        {
            heavy.Respawn();
            return;
        }

        LightPlayerController light = other.GetComponent<LightPlayerController>();
        if (light != null)
        {
            light.Respawn();
        }
    }
}