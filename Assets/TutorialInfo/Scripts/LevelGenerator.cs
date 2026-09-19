using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Colors")]
    public Color groundColor = Color.white;
    public Color breakableColor = new Color(1f, 0.5f, 0.3f);
    public Color heavyColor = new Color(0.3f, 0.5f, 1f);
    public Color lightColor = new Color(1f, 0.8f, 0.3f);
    public Color buttonColor = new Color(1f, 0.4f, 0.7f);
    public Color barrierColor = new Color(0.3f, 0.7f, 0.4f);
    public Color pushBoxColor = new Color(0.6f, 0.4f, 0.2f);

    void Start()
    {
        BuildLevel();
    }

    void BuildLevel()
    {
        // Ground segments
        CreateFloor("Floor_HeavySpawn", new Vector2(-8, -2), new Vector2(4, 0.5f), groundColor);
        CreateBreakableFloor("BreakableFloor", new Vector2(-3, -2), new Vector2(3, 0.5f));
        CreateFloor("Floor_Mid", new Vector2(1, -2), new Vector2(4, 0.5f), groundColor);
        CreateFloor("Floor_LightSpawn", new Vector2(6, -2), new Vector2(5, 0.5f), groundColor);

        // Hidden tunnel beneath the breakable section
        CreateFloor("Floor_Tunnel", new Vector2(-3, -5), new Vector2(3, 0.5f), groundColor);

        // Ceiling button + barrier puzzle
        GameObject barrier = CreateBarrier("VerticalBarrier", new Vector2(3.5f, -0.5f), new Vector2(0.5f, 3f));
        CreateCeilingButton("CeilingButton", new Vector2(3.5f, 3f), barrier);

        // Pushable box
        CreatePushBox("PushBox", new Vector2(0, -1));

        // Players
        CreateHeavyPlayer(new Vector2(-8, -0.5f));
        CreateLightPlayer(new Vector2(6, -0.5f));
    }

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

    void CreateFloor(string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = CreateSquare(name, position, size, color);
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
}
