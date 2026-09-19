using UnityEngine;

public class BreakableFloor : MonoBehaviour
{
    public float breakDelay = 0.5f;
    private bool breaking = false;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("HeavyPlayer") && !breaking)
        {
            breaking = true;
            Invoke("Break", breakDelay);
        }
    }

    void Break()
    {
        gameObject.SetActive(false);
    }
}