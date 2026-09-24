using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère les entrées du joueur via le nouveau système d'inputs d'Unity.
/// S'occupe du chargement et de la sauvegarde des reconfigurations de touches (rebinds) 
/// ainsi que de la bascule entre les différents contextes de contrôles (Gameplay vs UI).
/// </summary>
[RequireComponent(typeof(SaveManager))]
public class InputManager : CoreSystem
{
    #region Internal State
    [Header("Configuration")]
    [Tooltip("L'asset Input Actions principal du jeu contenant toutes les Action Maps et Bindings.")]
    [SerializeField]
    private InputActionAsset inputAsset;

    private SaveManager saveManager;
    #endregion

    #region Public Methods
    /// <summary>
    /// Initialise le gestionnaire, récupère les dépendances, charge les touches remappées par le joueur,
    /// et configure l'état par défaut des contrôles sur l'interface (UI).
    /// </summary>
    public override void Initialize()
    {
        saveManager = GetComponent<SaveManager>();

        if (inputAsset == null)
        {
            Debug.LogWarning("InputManager : Aucun InputActionAsset n'a été assigné !");
            IsReady = true;
            return;
        }

        LoadSavedBindings();

        // Par défaut au démarrage (souvent dans le menu principal), on active la navigation UI
        EnableUIMap();

        IsReady = true;
        Debug.Log("InputManager : Touches personnalisées chargées et Action Maps configurées.");
    }

    /// <summary>
    /// Sérialise en JSON toutes les modifications (overrides) apportées aux touches par le joueur
    /// et demande au SaveManager d'enregistrer ces données sur le disque.
    /// </summary>
    public void SaveCurrentBindings()
    {
        // Extraction des modifications depuis l'InputAsset natif
        string currentOverrides = inputAsset.SaveBindingOverridesAsJson();

        saveManager.CurrentSettings.inputBindings = currentOverrides;
        saveManager.SaveSettings();
    }

    /// <summary>
    /// Désactive les contrôles de menu et active les contrôles du personnage en jeu.
    /// À appeler lors de la fermeture d'un menu de pause ou au chargement d'un niveau.
    /// </summary>
    public void EnableGameplayMap()
    {
        inputAsset.FindActionMap("UI")?.Disable();
        inputAsset.FindActionMap("Gameplay")?.Enable();
    }

    /// <summary>
    /// Désactive les contrôles de jeu et active la navigation dans les menus.
    /// À appeler lors de l'ouverture de l'inventaire, d'un menu de pause, ou au retour au menu principal.
    /// </summary>
    public void EnableUIMap()
    {
        inputAsset.FindActionMap("Gameplay")?.Disable();
        inputAsset.FindActionMap("UI")?.Enable();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Lit la configuration JSON enregistrée dans la sauvegarde et l'applique à l'InputAsset
    /// pour écraser les touches par défaut avec les préférences du joueur.
    /// </summary>
    private void LoadSavedBindings()
    {
        string savedBindings = saveManager.CurrentSettings.inputBindings;

        if (!string.IsNullOrEmpty(savedBindings))
        {
            inputAsset.LoadBindingOverridesFromJson(savedBindings);
        }
    }
    #endregion
}