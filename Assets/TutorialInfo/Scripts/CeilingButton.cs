using UnityEngine;

public class CeilingButton : MonoBehaviour
{
    public GameObject barrierToRemove;
    private bool activated = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("LightPlayer") && !activated)
        {
            AudioManager.Instance.PlaySFX("button");
            activated = true;
            barrierToRemove.SetActive(false);
        }
    }
}