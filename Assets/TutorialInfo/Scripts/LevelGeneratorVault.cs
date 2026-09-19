using UnityEngine;

public class LevelGenerator_Vault : MonoBehaviour
{
    [Header("Colors")]
    public Color groundColor = Color.white;
    public Color breakableColor = new Color(1f, 0.5f, 0.3f);
    public Color heavyColor = new Color(0.3f, 0.5f, 1f);
    public Color lightColor = new Color(1f, 0.8f, 0.3f);
    public Color buttonColor = new Color(1f, 0.4f, 0.7f);
    public Color barrierColor = new Color(0.3f, 0.7f, 0.4f);
    public Color pushBoxColor = new Color(0.6f, 0.4f, 0.2f);
    public Color goalColor = new Color(0.9f, 0.85f, 0.2f);
    public Color collectibleColor = new Color(0.8f, 0.2f, 0.9f);

    void Start()
    {
        BuildLevel();
    }

    void BuildLevel()
    {
        // ===== LAYER 1: Entry Hall (y = 10) =====
        CreateFloor("Floor_L1_Start", new Vector2(-16, 10), new Vector2(8, 0.5f));
        CreateBreakableFloor("Breakable_L1_Drop", new Vector2(-8, 10), new Vector2(4, 0.5f));

        // ===== LAYER 2: The Chasm (y = 4) =====
        CreateFloor("Floor_L2_Landing", new Vector2(-8, 4), new Vector2(4, 0.5f));
        CreateFloor("PitFloor_L2", new Vector2(-4.5f, 3), new Vector2(3, 0.5f)); // shallow trench floor
        CreatePushBox("PushBox_1", new Vector2(-9, 4.6f));
        CreateFloor("Floor_L2_Mid", new Vector2(0, 4), new Vector2(6, 0.5f));

        GameObject barrier1 = CreateBarrier("Barrier_1", new Vector2(4, 4.5f), new Vector2(0.5f, 3));
        CreateCeilingButton("CeilingButton_1", new Vector2(1, 8), barrier1);

        CreateFloor("Floor_L2_End", new Vector2(6.75f, 4), new Vector2(4.5f, 0.5f));
        CreateBreakableFloor("Breakable_L2_Drop", new Vector2(10.5f, 4), new Vector2(3, 0.5f));

        // ===== LAYER 3: Vault Approach (y = -2) =====
        CreateFloor("Floor_L3_Landing", new Vector2(10.5f, -2), new Vector2(3, 0.5f));

        // Secret side room (optional detour, off the main path)
        CreateBreakableFloor("Breakable_Secret", new Vector2(7, -2), new Vector2(2, 0.5f));
        CreateFloor("Floor_Secret_Room", new Vector2(7.5f, -5), new Vector2(3, 0.5f));
        CreateCollectible("Collectible_1", new Vector2(7.5f, -4.5f));

        CreateFloor("Floor_L3_Main", new Vector2(15, -2), new Vector2(6, 0.5f));
        CreatePushBox("PushBox_2", new Vector2(13, -1.4f));

        GameObject barrier2 = CreateBarrier("Barrier_2", new Vector2(18.5f, -1.5f), new Vector2(0.5f, 3));
        CreateCeilingButton("CeilingButton_2", new Vector2(16, 2), barrier2);

        CreateFloor("Floor_L3_End", new Vector2(20.5f, -2), new Vector2(3, 0.5f));
        CreateBreakableFloor("Breakable_L3_Drop", new Vector2(23.5f, -2), new Vector2(3, 0.5f));

        // ===== LAYER 4: The Vault (y = -8) =====
        CreateFloor("Floor_L4_Vault", new Vector2(26, -8), new Vector2(8, 0.5f));
        CreateGoalZone("GoalZone", new Vector2(28, -7.25f), new Vector2(2, 1));

        // ===== Players (spawn together on Layer 1) =====
        CreateHeavyPlayer(new Vector2(-18, 11));
        CreateLightPlayer(new Vector2(-15, 11));
    }

    // ---------- Core builders ----------

    GameObject CreateSquare(string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        return go;
    }

    void CreateFloor(string name, Vector2 position, Vector2 size)
    {
        GameObject go = CreateSquare(name, position, size, groundColor);
        go.AddComponent<BoxCollider2D>();
        go.tag = "Ground";
    }

    void CreateBreakableFloor(string name, Vector2 position, Vector2 size)
    {
        GameObject go = CreateSquare(name, position, size, breakableColor);
        go.AddComponent<BoxCollider2D>();
        go.tag = "Ground";
        go.AddComponent<BreakableFloor>();
    }

    GameObject CreateBarrier(string name, Vector2 position, Vector2 size)
    {
        GameObject go = CreateSquare(name, position, size, barrierColor);
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    void CreateCeilingButton(string name, Vector2 position, GameObject barrier)
    {
        GameObject go = CreateSquare(name, position, new Vector2(1f, 0.5f), buttonColor);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        CeilingButton script = go.AddComponent<CeilingButton>();
        script.barrierToRemove = barrier;
    }

    void CreatePushBox(string name, Vector2 position)
    {
        GameObject go = CreateSquare(name, position, new Vector2(1f, 1f), pushBoxColor);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 2f;
        rb.linearDamping = 1f;
        go.AddComponent<BoxCollider2D>();
    }

    void CreateHeavyPlayer(Vector2 position)
    {
        GameObject go = CreateSquare("HeavyPlayer", position, new Vector2(1f, 1f), heavyColor);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        go.AddComponent<BoxCollider2D>();
        go.tag = "HeavyPlayer";
        go.AddComponent<HeavyPlayerController>();
    }

    void CreateLightPlayer(Vector2 position)
    {
        GameObject go = CreateSquare("LightPlayer", position, new Vector2(1f, 1f), lightColor);
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        go.AddComponent<BoxCollider2D>();
        go.tag = "LightPlayer";
        go.AddComponent<LightPlayerController>();
    }

    // ---------- New additions for this level ----------

    void CreateGoalZone(string name, Vector2 position, Vector2 size)
    {
        GameObject go = CreateSquare(name, position, size, goalColor);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        go.AddComponent<GoalZone>();
    }

    void CreateCollectible(string name, Vector2 position)
    {
        GameObject go = CreateSquare(name, position, new Vector2(0.5f, 0.5f), collectibleColor);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        go.AddComponent<CollectibleItem>();
    }
}

// Checks that both players are standing in the goal zone at the same time
public class GoalZone : MonoBehaviour
{
    private bool heavyInside = false;
    private bool lightInside = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HeavyPlayer")) heavyInside = true;
        if (other.CompareTag("LightPlayer")) lightInside = true;
        CheckComplete();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("HeavyPlayer")) heavyInside = false;
        if (other.CompareTag("LightPlayer")) lightInside = false;
    }

    void CheckComplete()
    {
        if (heavyInside && lightInside)
        {
            Debug.Log("Level complete! Both players reached the vault.");
        }
    }
}

// Simple pickup for the secret room bonus area
public class CollectibleItem : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HeavyPlayer") || other.CompareTag("LightPlayer"))
        {
            Debug.Log("Collectible found!");
            gameObject.SetActive(false);
        }
    }
}