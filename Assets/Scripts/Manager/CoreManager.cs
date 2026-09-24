using UnityEngine;
using System.Collections;

/// <summary>
/// Singleton persistant (DontDestroyOnLoad) servant de cœur architectural au jeu.
/// Il garantit l'initialisation séquentielle et sécurisée de tous les sous-systèmes vitaux (CoreSystems).
/// </summary>
public class CoreManager : MonoBehaviour
{
    #region Internal State
    /// <summary>
    /// Instance unique du CoreManager (Pattern Singleton).
    /// </summary>
    public static CoreManager Instance { get; private set; }

    /// <summary>
    /// Indique si la séquence d'initialisation de tous les sous-systèmes est terminée.
    /// </summary>
    public bool IsInitialized { get; private set; } = false;

    [Header("Sous-Systèmes")]
    [SerializeField]
    [Tooltip("Gestionnaire de sauvegarde, toujours initialisé en premier.")]
    private CoreSystem saveManager;

    [SerializeField]
    [Tooltip("Gestionnaire des paramètres graphiques.")]
    private CoreSystem graphicsManager;

    [SerializeField]
    [Tooltip("Gestionnaire du mixage audio.")]
    private CoreSystem audioManager;

    [SerializeField]
    [Tooltip("Gestionnaire des contrôles et du rebind.")]
    private CoreSystem inputManager;

    [SerializeField]
    [Tooltip("Gestionnaire des transitions de scènes.")]
    private CoreSystem transitionManager;
    #endregion

    #region Unity Life Cycle
    private void Awake()
    {
        // Implémentation stricte du Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Lance la séquence d'initialisation asynchrone des sous-systèmes.
    /// Appelée généralement par le Bootstrapper au démarrage du jeu.
    /// </summary>
    public void Initialize()
    {
        StartCoroutine(InitializationSequence());
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Gère l'ordre d'initialisation des CoreSystems.
    /// Force le SaveManager à être prêt avant de lancer les autres systèmes,
    /// car ils dépendent souvent des données chargées (ex: volume audio, résolution, touches).
    /// </summary>
    /// <returns>IEnumerator pour la coroutine d'attente.</returns>
    private IEnumerator InitializationSequence()
    {
        // Initialisation prioritaire du SaveManager
        if (saveManager != null)
        {
            saveManager.Initialize();
            yield return new WaitUntil(() => saveManager.IsReady);
        }
        else
        {
            Debug.LogError("CoreManager : SaveManager manquant !");
        }

        // Initialisation parallèle des autres systèmes
        if (graphicsManager != null) graphicsManager.Initialize();
        if (audioManager != null) audioManager.Initialize();
        if (inputManager != null) inputManager.Initialize();
        if (transitionManager != null) transitionManager.Initialize();

        // Attente de la complétion de tous les systèmes
        yield return new WaitUntil(() =>
            (graphicsManager == null || graphicsManager.IsReady) &&
            (audioManager == null || audioManager.IsReady) &&
            (inputManager == null || inputManager.IsReady) &&
            (transitionManager == null || transitionManager.IsReady)
        );

        IsInitialized = true;
        Debug.Log("CoreManager : Tous les sous-systèmes sont initialisés dans le bon ordre !");
    }
    #endregion
}