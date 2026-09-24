using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Gère la navigation dans le menu principal (Accueil, Options, etc.).
/// Utilise un système de pile (Stack) pour naviguer entre les panneaux et 
/// lance la transition vers le jeu ou gère la fermeture de l'application.
/// </summary>
public class MenuManager : MonoBehaviour
{
    #region Internal State
    [Header("Panneaux (Panels)")]
    [SerializeField]
    [Tooltip("Le panneau d'accueil principal du jeu.")]
    private GameObject mainMenuPanel;

    [SerializeField]
    [Tooltip("Le panneau contenant les paramètres et options.")]
    private GameObject optionsPanel;

    [SerializeField]
    [Tooltip("Le panneau de configuration des touches. Utilisé ici pour bloquer la touche Retour s'il est actif.")]
    private GameObject rebindPanel;

    [Header("Boutons par défaut (Manette)")]
    [SerializeField]
    [Tooltip("Le bouton Jouer, sélectionné par défaut à l'ouverture du menu.")]
    private GameObject playButton;

    [SerializeField]
    [Tooltip("Le premier élément à sélectionner à l'ouverture des options (généralement un bouton Retour ou un onglet).")]
    private GameObject optionsBackButton;

    [Header("Input - Bouton Retour")]
    [SerializeField]
    [Tooltip("Action déclenchant le retour en arrière dans les menus (ex: Bouton B/Rond, Échap).")]
    private InputActionReference cancelAction;

    /// <summary>
    /// Historique de navigation pour revenir en arrière proprement (dépilement).
    /// </summary>
    private Stack<GameObject> menuStack = new Stack<GameObject>();
    #endregion

    #region Unity Life Cycle
    private void OnEnable()
    {
        if (cancelAction != null)
        {
            cancelAction.action.Enable();
            cancelAction.action.performed += OnCancelPressed;
        }
    }

    private void OnDisable()
    {
        if (cancelAction != null)
        {
            cancelAction.action.performed -= OnCancelPressed;
            cancelAction.action.Disable();
        }
    }

    private void Start()
    {
        // Initialisation de l'état des panneaux
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        // Ajoute le menu principal comme base de la pile de navigation
        menuStack.Push(mainMenuPanel);

        // Force le focus sur le bouton Jouer pour les joueurs manette/clavier
        EventSystem.current.SetSelectedGameObject(playButton);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Ouvre le menu des options en l'empilant par-dessus le menu actuel.
    /// </summary>
    public void OpenOptionsPanel()
    {
        OpenMenu(optionsPanel, optionsBackButton);
    }

    /// <summary>
    /// Dépile le panneau actuel et réaffiche le précédent.
    /// Si le joueur est revenu au menu principal, restaure le focus sur le bouton Jouer.
    /// </summary>
    public void GoBack()
    {
        if (menuStack.Count <= 1) return;

        GameObject currentMenu = menuStack.Pop();
        currentMenu.SetActive(false);

        GameObject previousMenu = menuStack.Peek();
        previousMenu.SetActive(true);

        if (previousMenu == mainMenuPanel)
        {
            EventSystem.current.SetSelectedGameObject(playButton);
        }
    }

    /// <summary>
    /// Retire le focus UI pour éviter les interactions indésirables, 
    /// puis demande au TransitionManager de lancer le chargement du premier niveau.
    /// </summary>
    public void PlayGame()
    {
        EventSystem.current.SetSelectedGameObject(null);

        Debug.Log("Lancement de la transition vers le jeu...");
        CoreManager.Instance.GetComponent<TransitionManager>().LoadLevel("Level1");
    }

    /// <summary>
    /// Sauvegarde les paramètres globaux (au cas où ils auraient été modifiés)
    /// et quitte proprement l'application ou arrête le mode Play de l'éditeur.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Fermeture du jeu.");

        // Sauvegarde de sécurité avant de quitter
        CoreManager.Instance.GetComponent<SaveManager>().SaveSettings();

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Méthode utilitaire interne pour ouvrir un nouveau panneau, masquer le précédent
    /// et appliquer le focus pour la navigation à la manette.
    /// </summary>
    /// <param name="panelToOpen">Le panneau à afficher.</param>
    /// <param name="firstSelectedButton">Le bouton à cibler avec l'EventSystem.</param>
    private void OpenMenu(GameObject panelToOpen, GameObject firstSelectedButton)
    {
        if (menuStack.Count > 0)
        {
            menuStack.Peek().SetActive(false);
        }

        panelToOpen.SetActive(true);
        menuStack.Push(panelToOpen);

        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }

    /// <summary>
    /// Intercepte l'appui sur le bouton Retour et déclenche la fonction GoBack.
    /// Bloque l'action si le panneau de reconfiguration des touches (Rebind) est ouvert.
    /// </summary>
    /// <param name="context">Contexte de l'action de l'InputSystem.</param>
    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        // SÉCURITÉ : Laisse le RebindManager gérer son propre retour pour éviter l'UI Bleeding.
        if (rebindPanel != null && rebindPanel.activeInHierarchy) return;

        GoBack();
    }
    #endregion
}