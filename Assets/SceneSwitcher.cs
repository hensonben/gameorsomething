using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to an empty GameObject. Watches two specific "portal" trigger
/// colliders and two specific "player" colliders, and loads a scene once
/// BOTH correct pairs are touching at the same time
/// (lightPlayer <-> lightPortal AND heavyPlayer <-> heavyPortal).
///
/// This does NOT require any script on the portals or players themselves --
/// just drag their Collider2D components into the fields below. The portal
/// colliders should have "Is Trigger" checked; the player colliders can be
/// triggers or solid, either works with Physics2D.IsTouching.
///
/// Make sure the target scene is added to File > Build Settings > Scenes In
/// Build, or SceneManager.LoadScene will fail at runtime.
/// </summary>
public class SceneSwitcher : MonoBehaviour
{
    [Header("Portals (trigger zones)")]
    [Tooltip("The trigger collider that only the light player should enter.")]
    [SerializeField] private Collider2D lightPortal;

    [Tooltip("The trigger collider that only the heavy player should enter.")]
    [SerializeField] private Collider2D heavyPortal;

    [Header("Players (must match specific objects, not just any object)")]
    [Tooltip("The specific light player's collider.")]
    [SerializeField] private Collider2D lightPlayer;

    [Tooltip("The specific heavy player's collider.")]
    [SerializeField] private Collider2D heavyPlayer;

    [Header("Scene To Load")]
    [Tooltip("Exact name of the scene to load. Must be added to Build " +
             "Settings > Scenes In Build.")]
    [SerializeField] private string sceneToLoad;

    [Tooltip("Optional delay (seconds) between both portals being activated " +
             "and the scene actually loading. Useful for playing a fade-out " +
             "or effect first via onBothPortalsActivated below. 0 = instant.")]
    [SerializeField] private float loadDelay = 0f;

    [Header("Events")]
    [Tooltip("Fires the instant both correct pairs are touching, before the " +
             "load delay. Hook up a fade-to-black or sound effect here.")]
    public UnityEvent onBothPortalsActivated;

    private bool hasTriggered;

    private void Awake()
    {
        if (lightPortal == null || heavyPortal == null || lightPlayer == null || heavyPlayer == null)
        {
            Debug.LogWarning($"{name}: DualPortalSceneSwitcher is missing one or more " +
                              "collider references. It will not function until all four are assigned.");
        }

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning($"{name}: DualPortalSceneSwitcher has no sceneToLoad set.");
        }
    }

    private void Update()
    {
        if (hasTriggered) return;
        if (lightPortal == null || heavyPortal == null || lightPlayer == null || heavyPlayer == null) return;

        bool lightMatched = lightPortal.IsTouching(lightPlayer);
        bool heavyMatched = heavyPortal.IsTouching(heavyPlayer);

        if (lightMatched && heavyMatched)
        {
            hasTriggered = true;
            onBothPortalsActivated?.Invoke();

            if (loadDelay <= 0f)
            {
                LoadTargetScene();
            }
            else
            {
                Invoke(nameof(LoadTargetScene), loadDelay);
            }
        }
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError($"{name}: Cannot load scene, sceneToLoad is empty.");
            return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}