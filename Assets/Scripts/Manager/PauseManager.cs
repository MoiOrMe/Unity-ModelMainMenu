using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Gère la boucle de pause du jeu, l'arrêt du temps (TimeScale), et la navigation 
/// dans les différents sous-menus (Options, Rebind) via un système de pile (Stack).
/// </summary>
public class PauseManager : MonoBehaviour
{
    #region Internal State
    [Header("Panneaux")]
    [SerializeField]
    [Tooltip("Le conteneur racine de toute l'UI de pause.")]
    private GameObject pauseRoot;

    [SerializeField]
    [Tooltip("Le panneau principal de la pause (Reprendre, Options, Quitter).")]
    private GameObject pauseMainPanel;

    [SerializeField]
    [Tooltip("Le panneau des options.")]
    private GameObject optionsPanel;

    [SerializeField]
    [Tooltip("Le panneau de configuration des touches. Utilisé ici pour bloquer la touche Retour s'il est actif.")]
    private GameObject rebindPanel;

    [Header("Navigation Manette")]
    [SerializeField]
    [Tooltip("Le premier bouton à sélectionner lors de l'ouverture du menu de pause.")]
    private GameObject resumeButton;

    [SerializeField]
    [Tooltip("Le premier élément à sélectionner lors de l'ouverture du menu des options.")]
    private GameObject optionsFirstElement;

    [Header("Inputs")]
    [SerializeField]
    [Tooltip("Action déclenchant la mise en pause (ex: Start / Echap).")]
    private InputActionReference pauseAction;

    [SerializeField]
    [Tooltip("Action déclenchant le retour en arrière (ex: Bouton B/Rond).")]
    private InputActionReference cancelAction;

    private bool isPaused = false;

    /// <summary>
    /// Historique de navigation : stocke l'ordre des menus ouverts pour pouvoir revenir en arrière proprement.
    /// </summary>
    private Stack<GameObject> menuStack = new Stack<GameObject>();
    #endregion

    #region Unity Life Cycle
    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePressed;
        }
        if (cancelAction != null)
        {
            cancelAction.action.Enable();
            cancelAction.action.performed += OnCancelPressed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
            pauseAction.action.Disable();
        }
        if (cancelAction != null)
        {
            cancelAction.action.performed -= OnCancelPressed;
            cancelAction.action.Disable();
        }
    }

    private void Start()
    {
        // On s'assure que tout est bien masqué au lancement de la scène
        pauseRoot.SetActive(false);
        optionsPanel.SetActive(false);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Met le jeu en pause, arrête l'écoulement du temps, bascule l'InputSystem en mode UI,
    /// et affiche le menu principal de la pause.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        CoreManager.Instance.GetComponent<InputManager>().EnableUIMap();

        pauseRoot.SetActive(true);
        OpenMenu(pauseMainPanel, resumeButton);
    }

    /// <summary>
    /// Relance le jeu, rétablit l'écoulement du temps, repasse l'InputSystem en mode Gameplay,
    /// et vide l'historique des menus de pause.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        CoreManager.Instance.GetComponent<InputManager>().EnableGameplayMap();

        menuStack.Clear();
        pauseRoot.SetActive(false);
    }

    /// <summary>
    /// Ouvre le sous-menu des options en l'ajoutant à la pile de navigation.
    /// </summary>
    public void OpenOptions()
    {
        OpenMenu(optionsPanel, optionsFirstElement);
    }

    /// <summary>
    /// Dépile le menu actuel pour retourner au menu précédent. 
    /// Si le joueur est sur le menu principal de la pause, cette fonction ne fait rien.
    /// </summary>
    public void GoBack()
    {
        if (menuStack.Count <= 1) return;

        // On retire et masque le menu actuel
        GameObject currentMenu = menuStack.Pop();
        currentMenu.SetActive(false);

        // On réaffiche le menu précédent
        GameObject previousMenu = menuStack.Peek();
        previousMenu.SetActive(true);

        // Si on est de retour sur le menu principal, on replace le focus sur "Reprendre"
        if (previousMenu == pauseMainPanel)
        {
            EventSystem.current.SetSelectedGameObject(resumeButton);
        }
    }

    /// <summary>
    /// Rétablit le temps normal, sauvegarde les paramètres actuels, ferme la session de la sauvegarde
    /// et déclenche la transition vers le menu principal.
    /// </summary>
    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        CoreManager.Instance.GetComponent<SaveManager>().SaveSettings();
        CoreManager.Instance.GetComponent<SaveManager>().CloseCurrentSession();
        CoreManager.Instance.GetComponent<TransitionManager>().LoadLevel("MainMenu");
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Callback invoqué lors de l'appui sur la touche de Pause.
    /// </summary>
    private void OnPausePressed(InputAction.CallbackContext context)
    {
        // On empêche de remettre en pause un jeu déjà en pause (le retour jeu se fait via ResumeGame ou Cancel)
        if (!isPaused)
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Callback invoqué lors de l'appui sur la touche Retour (Cancel).
    /// </summary>
    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        // SÉCURITÉ : Si le panneau de Rebind est ouvert, on ignore l'input de retour.
        // Le RebindManager gère lui-même sa propre fermeture pour éviter la désynchronisation.
        if (rebindPanel != null && rebindPanel.activeInHierarchy) return;

        if (menuStack.Count > 1)
        {
            GoBack();
        }
        else if (menuStack.Count == 1 && isPaused)
        {
            ResumeGame();
        }
    }

    /// <summary>
    /// Méthode utilitaire pour ouvrir un menu, l'empiler dans l'historique et gérer le focus manette.
    /// </summary>
    /// <param name="panelToOpen">Le GameObject du panneau à afficher.</param>
    /// <param name="firstSelectedButton">Le premier élément UI à cibler avec l'EventSystem.</param>
    private void OpenMenu(GameObject panelToOpen, GameObject firstSelectedButton)
    {
        // On masque le menu précédent s'il y en a un
        if (menuStack.Count > 0)
        {
            menuStack.Peek().SetActive(false);
        }

        panelToOpen.SetActive(true);
        menuStack.Push(panelToOpen);

        // Réinitialise le focus pour éviter un conflit visuel, puis assigne le nouveau focus
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }
    #endregion
}