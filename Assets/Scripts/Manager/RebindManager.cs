using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Gère la configuration des touches (Rebind) du jeu.
/// Construit l'interface dynamiquement en lisant l'InputActionAsset, gère la navigation manette
/// explicite générée à la volée, et s'occupe de l'enregistrement des nouvelles touches.
/// </summary>
public class RebindManager : MonoBehaviour
{
    #region Internal State
    [Header("Configuration")]
    [SerializeField]
    [Tooltip("L'asset Input Actions contenant toutes les touches du jeu.")]
    private InputActionAsset inputAsset;

    [SerializeField]
    [Tooltip("Le prefab représentant une ligne d'action (Texte + Bouton Clavier + Bouton Manette).")]
    private GameObject rebindRowPrefab;

    [SerializeField]
    [Tooltip("Le panneau global du Rebind (lui-même).")]
    private GameObject rebindPanel;

    [Header("Exclusions")]
    [SerializeField]
    [Tooltip("Liste des noms d'actions à ignorer (ex: 'Look' pour le joystick de la caméra qu'on ne veut pas remapper).")]
    private List<string> actionsToIgnore = new List<string> { "Look" };

    [Header("Input - Changement d'Onglet")]
    [SerializeField] private InputActionReference previousTabAction;
    [SerializeField] private InputActionReference nextTabAction;
    [SerializeField] private InputActionReference cancelAction;

    [Header("Système d'Onglets")]
    [SerializeField] private GameObject gameplayScrollView;
    [SerializeField] private GameObject uiScrollView;
    [SerializeField] private Image gameplayTabImage;
    [SerializeField] private Image uiTabImage;
    [SerializeField] private Color activeTabColor = new Color(0f, 0.64f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(1f, 1f, 1f);

    [Header("Zones d'instanciation")]
    [SerializeField]
    [Tooltip("Le conteneur (Content) de la ScrollView du Gameplay.")]
    private Transform gameplayContent;

    [SerializeField]
    [Tooltip("Le conteneur (Content) de la ScrollView de l'UI.")]
    private Transform uiContent;

    [Header("Boutons Globaux")]
    [SerializeField] private Button tabGameplayButton;
    [SerializeField] private Button tabUIButton;
    [SerializeField] private Button validateButton;
    [SerializeField] private Button resetButton;

    [Header("Navigation de Retour")]
    [SerializeField] private GameObject parentOptionsPanel;
    [SerializeField] private GameObject buttonToFocusOnClose;

    [Header("UI d'attente")]
    [SerializeField]
    [Tooltip("Le panneau noir semi-transparent qui bloque l'écran pendant qu'on attend la saisie du joueur.")]
    private GameObject waitingOverlay;

    [SerializeField]
    [Tooltip("Le texte affiché pendant l'attente de la saisie.")]
    private TextMeshProUGUI waitingText;

    /// <summary>
    /// L'opération asynchrone native de l'InputSystem qui écoute la nouvelle touche.
    /// </summary>
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    private List<RebindRowUI> gameplayRows = new List<RebindRowUI>();
    private List<RebindRowUI> uiRows = new List<RebindRowUI>();

    private int currentTabIndex = 0;
    #endregion

    #region Unity Life Cycle
    private void OnEnable()
    {
        if (previousTabAction != null) previousTabAction.action.performed += OnPreviousTab;
        if (nextTabAction != null) nextTabAction.action.performed += OnNextTab;
        if (cancelAction != null) cancelAction.action.performed += OnCancelAction;

        waitingOverlay.SetActive(false);

        // Génère l'UI à chaque ouverture pour s'assurer d'avoir les données les plus récentes
        GenerateUI();
        OpenGameplayTab();
    }

    private void OnDisable()
    {
        if (previousTabAction != null) previousTabAction.action.performed -= OnPreviousTab;
        if (nextTabAction != null) nextTabAction.action.performed -= OnNextTab;
        if (cancelAction != null) cancelAction.action.performed -= OnCancelAction;
    }
    #endregion

    #region Public Methods (UI Callbacks)
    /// <summary>
    /// Ouvre l'onglet regroupant les touches liées au gameplay (déplacement, actions en jeu).
    /// </summary>
    public void OpenGameplayTab()
    {
        currentTabIndex = 0;
        SwitchTab(gameplayScrollView, gameplayTabImage, gameplayRows.Count > 0 ? gameplayRows[0].gamepadButton.gameObject : null);
        UpdateBottomButtonsNavigation(gameplayRows);
    }

    /// <summary>
    /// Ouvre l'onglet regroupant les touches liées à l'interface (navigation menu).
    /// </summary>
    public void OpenUITab()
    {
        currentTabIndex = 1;
        SwitchTab(uiScrollView, uiTabImage, uiRows.Count > 0 ? uiRows[0].gamepadButton.gameObject : null);
        UpdateBottomButtonsNavigation(uiRows);
    }

    /// <summary>
    /// Enregistre toutes les modifications dans la sauvegarde (via le gestionnaire global) et ferme le menu.
    /// </summary>
    public void ValidateAndClose()
    {
        CoreManager.Instance.GetComponent<InputManager>().SaveCurrentBindings();
        CloseRebindMenu();
    }

    /// <summary>
    /// Ferme le panneau de Rebind, réaffiche le panneau parent des Options et redonne le focus manette initial.
    /// </summary>
    public void CloseRebindMenu()
    {
        if (parentOptionsPanel != null) parentOptionsPanel.SetActive(true);

        rebindPanel.SetActive(false);

        if (buttonToFocusOnClose != null && buttonToFocusOnClose.activeInHierarchy)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(buttonToFocusOnClose);
        }
    }

    /// <summary>
    /// Supprime toutes les surcharges (overrides) du joueur pour restaurer la configuration par défaut de l'InputAsset.
    /// </summary>
    public void ResetBindingsToDefault()
    {
        inputAsset.RemoveAllBindingOverrides();
        CoreManager.Instance.GetComponent<InputManager>().SaveCurrentBindings();
        GenerateUI();
    }
    #endregion

    #region Private Methods
    private void OnPreviousTab(InputAction.CallbackContext ctx) => CycleTab(-1);
    private void OnNextTab(InputAction.CallbackContext ctx) => CycleTab(1);

    /// <summary>
    /// Intercepte le bouton retour physique. Bloque le retour si le joueur est en train d'assigner une touche.
    /// </summary>
    private void OnCancelAction(InputAction.CallbackContext ctx)
    {
        if (!waitingOverlay.activeSelf)
        {
            CloseRebindMenu();
        }
    }

    /// <summary>
    /// Fait tourner les onglets de gauche à droite ou de droite à gauche.
    /// </summary>
    /// <param name="direction">-1 pour précédent, 1 pour suivant.</param>
    private void CycleTab(int direction)
    {
        currentTabIndex += direction;
        if (currentTabIndex > 1) currentTabIndex = 0;
        if (currentTabIndex < 0) currentTabIndex = 1;

        if (currentTabIndex == 0) OpenGameplayTab();
        else OpenUITab();
    }

    /// <summary>
    /// Gère la logique d'affichage visuel lors de la transition entre deux onglets.
    /// </summary>
    private void SwitchTab(GameObject viewToOpen, Image tabImageToHighlight, GameObject firstElementToFocus)
    {
        if (gameplayScrollView != null) gameplayScrollView.SetActive(false);
        if (uiScrollView != null) uiScrollView.SetActive(false);
        if (viewToOpen != null) viewToOpen.SetActive(true);

        if (gameplayTabImage != null) gameplayTabImage.color = inactiveTabColor;
        if (uiTabImage != null) uiTabImage.color = inactiveTabColor;
        if (tabImageToHighlight != null) tabImageToHighlight.color = activeTabColor;

        if (firstElementToFocus != null && firstElementToFocus.activeInHierarchy)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstElementToFocus);
        }
    }

    /// <summary>
    /// Détruit l'ancienne interface et reconstruit chaque ligne en parcourant l'InputAsset map par map.
    /// </summary>
    private void GenerateUI()
    {
        // Nettoyage de l'ancienne UI
        foreach (Transform child in gameplayContent) Destroy(child.gameObject);
        foreach (Transform child in uiContent) Destroy(child.gameObject);
        gameplayRows.Clear();
        uiRows.Clear();

        // Génération de la section Gameplay
        InputActionMap gameplayMap = inputAsset.FindActionMap("Gameplay");
        if (gameplayMap != null) GenerateRowsForMap(gameplayMap, gameplayContent, gameplayRows);

        // Génération de la section UI
        InputActionMap uiMap = inputAsset.FindActionMap("UI");
        if (uiMap != null) GenerateRowsForMap(uiMap, uiContent, uiRows);

        // Recalcul des liens de navigation explicite pour la manette
        ConfigureExplicitNavigation();
    }

    /// <summary>
    /// Parcourt toutes les actions d'une ActionMap pour instancier les lignes correspondantes dans l'interface.
    /// Gère notamment l'extraction complexe des bindings "Composites" (ex: ZQSD ou D-Pad).
    /// </summary>
    private void GenerateRowsForMap(InputActionMap map, Transform parentContent, List<RebindRowUI> targetList)
    {
        foreach (InputAction action in map.actions)
        {
            if (actionsToIgnore.Contains(action.name)) continue;

            List<string> rowNames = new List<string>();

            // Phase 1 : Extraction des noms d'actions et des sous-composites
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite) continue; // On ignore la racine du composite

                string rowName = action.name;
                if (binding.isPartOfComposite) rowName += " " + binding.name;

                if (!rowNames.Contains(rowName)) rowNames.Add(rowName);
            }

            // Phase 2 : Instanciation des prefabs pour chaque nom unique trouvé
            foreach (string rowName in rowNames)
            {
                GameObject newRowObj = Instantiate(rebindRowPrefab, parentContent);
                RebindRowUI rowUI = newRowObj.GetComponent<RebindRowUI>();

                rowUI.actionNameText.text = rowName;

                int kbIndex = -1;
                int gpIndex = -1;

                // Recherche des index exacts pour associer le bon bouton à la bonne ligne
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    InputBinding binding = action.bindings[i];
                    if (binding.isComposite) continue;

                    string currentBindingName = action.name + (binding.isPartOfComposite ? " " + binding.name : "");

                    if (currentBindingName == rowName)
                    {
                        if (binding.groups.Contains("Keyboard&Mouse")) kbIndex = i;
                        else if (binding.groups.Contains("Gamepad")) gpIndex = i;
                    }
                }

                ConfigureButton(rowUI.keyboardButton, rowUI.keyboardButtonText, action, kbIndex, "Keyboard");
                ConfigureButton(rowUI.gamepadButton, rowUI.gamepadButtonText, action, gpIndex, "Gamepad");

                targetList.Add(rowUI);
            }
        }
    }

    /// <summary>
    /// Configure un bouton UI spécifique (Clavier ou Manette), écrit sa valeur textuelle, 
    /// et lui attache l'écouteur d'événement pour lancer le rebind.
    /// </summary>
    private void ConfigureButton(Button button, TextMeshProUGUI text, InputAction action, int bindingIndex, string deviceType)
    {
        if (bindingIndex != -1)
        {
            text.text = action.GetBindingDisplayString(bindingIndex);
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => StartRebind(action, bindingIndex, text, deviceType));
        }
        else
        {
            // Désactive le bouton si l'action n'a pas de touche pour ce type de périphérique
            button.interactable = false;
            text.text = "N/A";
        }
    }

    /// <summary>
    /// Lance la configuration de la navigation (Haut/Bas) pour que l'EventSystem ne se perde pas dans la ScrollView.
    /// </summary>
    private void ConfigureExplicitNavigation()
    {
        LinkListNavigation(gameplayRows, tabGameplayButton);
        LinkListNavigation(uiRows, tabUIButton);
    }

    /// <summary>
    /// Calcule mathématiquement et force les chemins "SelectOnUp" et "SelectOnDown" pour chaque bouton généré.
    /// Assure une navigation manette sans failles, même avec des éléments désactivés (N/A) au milieu.
    /// </summary>
    private void LinkListNavigation(List<RebindRowUI> rows, Button parentTabButton)
    {
        if (rows.Count == 0) return;

        for (int i = 0; i < rows.Count; i++)
        {
            // Les boutons clavier ne sont pas navigables à la manette pour simplifier l'ergonomie
            Navigation kbNav = rows[i].keyboardButton.navigation;
            kbNav.mode = Navigation.Mode.None;
            rows[i].keyboardButton.navigation = kbNav;

            if (!rows[i].gamepadButton.interactable)
            {
                Navigation disabledNav = rows[i].gamepadButton.navigation;
                disabledNav.mode = Navigation.Mode.None;
                rows[i].gamepadButton.navigation = disabledNav;
                continue;
            }

            Navigation gpNav = rows[i].gamepadButton.navigation;
            gpNav.mode = Navigation.Mode.Explicit;

            // Détermination de la cible vers le HAUT
            Selectable upTarget = parentTabButton;
            for (int j = i - 1; j >= 0; j--)
            {
                if (rows[j].gamepadButton.interactable) { upTarget = rows[j].gamepadButton; break; }
            }
            gpNav.selectOnUp = upTarget;

            // Détermination de la cible vers le BAS
            Selectable downTarget = validateButton;
            for (int j = i + 1; j < rows.Count; j++)
            {
                if (rows[j].gamepadButton.interactable) { downTarget = rows[j].gamepadButton; break; }
            }
            gpNav.selectOnDown = downTarget;

            gpNav.selectOnLeft = null;
            gpNav.selectOnRight = null;

            rows[i].gamepadButton.navigation = gpNav;
        }
    }

    /// <summary>
    /// Met à jour la cible "SelectOnUp" des boutons en bas de l'écran (Valider / Réinitialiser)
    /// pour qu'ils pointent vers la dernière ligne active de l'onglet courant.
    /// </summary>
    private void UpdateBottomButtonsNavigation(List<RebindRowUI> currentRows)
    {
        Selectable lastValidRowButton = null;
        for (int i = currentRows.Count - 1; i >= 0; i--)
        {
            if (currentRows[i].gamepadButton.interactable)
            {
                lastValidRowButton = currentRows[i].gamepadButton;
                break;
            }
        }

        Navigation resetNav = resetButton.navigation;
        resetNav.mode = Navigation.Mode.Explicit;
        resetNav.selectOnRight = validateButton;
        resetNav.selectOnLeft = null;
        resetNav.selectOnUp = lastValidRowButton;
        resetNav.selectOnDown = null;
        resetButton.navigation = resetNav;

        Navigation valNav = validateButton.navigation;
        valNav.mode = Navigation.Mode.Explicit;
        valNav.selectOnLeft = resetButton;
        valNav.selectOnRight = null;
        valNav.selectOnUp = lastValidRowButton;
        valNav.selectOnDown = null;
        validateButton.navigation = valNav;
    }

    /// <summary>
    /// Démarre le processus asynchrone d'écoute de l'InputSystem pour capter la nouvelle touche du joueur.
    /// </summary>
    /// <param name="actionToRebind">L'action à modifier (ex: "Sauter").</param>
    /// <param name="bindingIndex">L'index précis du binding (ex: Clavier ou Manette).</param>
    /// <param name="textToUpdate">Le composant texte de l'UI à mettre à jour avec la nouvelle touche.</param>
    /// <param name="deviceType">Le type de périphérique attendu ("Keyboard" ou "Gamepad").</param>
    private void StartRebind(InputAction actionToRebind, int bindingIndex, TextMeshProUGUI textToUpdate, string deviceType)
    {
        actionToRebind.Disable(); // Sécurité obligatoire par l'InputSystem avant un rebind
        waitingOverlay.SetActive(true);
        waitingText.text = $"Appuyez sur une touche pour [{actionToRebind.name}]...";

        // Configuration et lancement de l'écoute
        rebindingOperation = actionToRebind.PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath($"<{deviceType}>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f) // Tolérance pour les touches qui "tremblent"
            .OnComplete(operation => RebindComplete(actionToRebind, bindingIndex, textToUpdate, operation))
            .OnCancel(operation => RebindCanceled(actionToRebind, operation))
            .Start();
    }

    /// <summary>
    /// Callback appelé par l'InputSystem lorsqu'une touche valide a été pressée et acceptée.
    /// </summary>
    private void RebindComplete(InputAction action, int bindingIndex, TextMeshProUGUI textToUpdate, InputActionRebindingExtensions.RebindingOperation operation)
    {
        textToUpdate.text = action.GetBindingDisplayString(bindingIndex);
        CleanUpRebind(action, operation);
    }

    /// <summary>
    /// Callback appelé par l'InputSystem si le joueur annule l'opération (ex: avec la touche Échap).
    /// </summary>
    private void RebindCanceled(InputAction action, InputActionRebindingExtensions.RebindingOperation operation)
    {
        CleanUpRebind(action, operation);
    }

    /// <summary>
    /// Nettoie la mémoire, réactive l'action en jeu et ferme le voile noir d'attente.
    /// </summary>
    private void CleanUpRebind(InputAction action, InputActionRebindingExtensions.RebindingOperation operation)
    {
        waitingOverlay.SetActive(false);
        action.Enable();
        operation.Dispose();
    }
    #endregion
}