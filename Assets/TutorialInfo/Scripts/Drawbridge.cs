using UnityEngine;

public class Drawbridge : MonoBehaviour
{
    public float fallAngle = -90f;
    public float fallSpeed = 90f;
    private bool falling = false;
    private bool hasFallen = false;

    void Update()
    {
        if (!falling) return;

        float newZ = Mathf.MoveTowardsAngle(transform.eulerAngles.z, fallAngle, fallSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(0, 0, newZ);

        if (Mathf.Approximately(newZ, fallAngle))
        {
            falling = false;
            hasFallen = true;
        }
    }

    public void TriggerFall()
    {
        if (!hasFallen && !falling)
            falling = true;
    }
}