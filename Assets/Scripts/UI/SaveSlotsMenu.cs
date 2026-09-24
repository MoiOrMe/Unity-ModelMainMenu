using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Gère l'interface de sélection des emplacements de sauvegarde (Save Slots).
/// Gère la création de nouvelles parties, le chargement, la suppression, 
/// et génère dynamiquement une navigation manette stricte pour éviter les bugs de l'EventSystem.
/// </summary>
public class SaveSlotsMenu : MonoBehaviour
{
    #region Internal State
    [Header("Navigation")]
    [SerializeField]
    [Tooltip("Le panneau du menu principal vers lequel retourner.")]
    private GameObject mainMenuPanel;

    [SerializeField]
    [Tooltip("Le panneau contenant ce menu des sauvegardes.")]
    private GameObject saveSlotsPanel;

    [SerializeField]
    [Tooltip("Le bouton (ex: 'Jouer') à recibler lors de la fermeture de ce menu.")]
    private GameObject playButtonToFocusOnClose;

    [SerializeField]
    [Tooltip("Le bouton de retour en bas de l'écran.")]
    private Button backButton;

    [Header("Les 3 Slots")]
    [SerializeField]
    [Tooltip("Tableau contenant les scripts UI gérant chaque emplacement individuel.")]
    private SaveSlotUI[] saveSlots;

    [Header("Confirmation de Suppression")]
    [SerializeField]
    [Tooltip("Le panneau modale demandant 'Êtes-vous sûr ?'")]
    private GameObject deleteConfirmationPanel;

    [SerializeField]
    [Tooltip("Le bouton Oui pour confirmer la suppression.")]
    private Button confirmDeleteButton;

    [SerializeField]
    [Tooltip("Le bouton Non pour annuler la suppression.")]
    private Button cancelDeleteButton;

    [SerializeField]
    [Tooltip("Le CanvasGroup englobant les slots, utilisé pour bloquer les clics en arrière-plan.")]
    private CanvasGroup slotsContainerGroup;

    private SaveManager saveManager;
    private TransitionManager transitionManager;

    /// <summary>
    /// Mémorise l'index de la sauvegarde en cours de suppression.
    /// </summary>
    private int pendingDeleteSlotIndex = -1;

    /// <summary>
    /// Mémorise le dernier bouton sélectionné pour lui redonner le focus si on annule la suppression.
    /// </summary>
    private GameObject previousFocusBeforeDelete;
    #endregion

    #region Public Methods (UI Callbacks)
    /// <summary>
    /// Ouvre le menu des sauvegardes, masque le menu principal et rafraîchit l'affichage de chaque emplacement.
    /// </summary>
    public void OpenSaveSlotsMenu()
    {
        mainMenuPanel.SetActive(false);
        saveSlotsPanel.SetActive(true);
        if (deleteConfirmationPanel != null) deleteConfirmationPanel.SetActive(false);

        InitializeManagers();
        RefreshAllSlots();

        // Réinitialise et force le focus sur le premier slot
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(saveSlots[0].gameObject);
    }

    /// <summary>
    /// Ferme le menu des sauvegardes et retourne au menu d'accueil.
    /// </summary>
    public void CloseSaveSlotsMenu()
    {
        saveSlotsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(playButtonToFocusOnClose);
    }

    /// <summary>
    /// Appelé lorsqu'un joueur clique sur le bouton principal d'un emplacement.
    /// Crée une nouvelle partie si le slot est vide, ou charge la partie existante.
    /// </summary>
    /// <param name="slotIndex">L'index de l'emplacement cliqué.</param>
    /// <param name="isEmpty">Vrai si le slot ne contient aucune sauvegarde.</param>
    /// <param name="data">Les données de la sauvegarde (null si le slot est vide).</param>
    public void OnSlotClicked(int slotIndex, bool isEmpty, GameData data)
    {
        if (isEmpty)
        {
            saveManager.CreateNewGame(slotIndex, "Level1");
            transitionManager.LoadLevel("Level1");
        }
        else
        {
            saveManager.LoadGame(slotIndex);
            transitionManager.LoadLevel(data.currentLevelName);
        }
    }

    /// <summary>
    /// Appelé lorsqu'un joueur clique sur la corbeille d'un emplacement.
    /// Ouvre la pop-up de confirmation et verrouille l'arrière-plan.
    /// </summary>
    /// <param name="slotIndex">L'index de l'emplacement à supprimer.</param>
    public void OnDeleteClicked(int slotIndex)
    {
        pendingDeleteSlotIndex = slotIndex;
        previousFocusBeforeDelete = EventSystem.current.currentSelectedGameObject;

        deleteConfirmationPanel.SetActive(true);

        // Bloque l'interactivité des slots derrière la fenêtre modale (évite le "Ghost Navigation")
        if (slotsContainerGroup != null) slotsContainerGroup.interactable = false;

        LockPopupNavigation();

        // Donne le focus au bouton "Non" par sécurité
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(cancelDeleteButton.gameObject);
    }

    /// <summary>
    /// Confirme la suppression, efface le fichier sur le disque, et ferme la pop-up.
    /// </summary>
    public void ConfirmDelete()
    {
        if (pendingDeleteSlotIndex != -1)
        {
            saveManager.DeleteSave(pendingDeleteSlotIndex);
            RefreshAllSlots();
        }
        CloseDeleteConfirmation();
    }

    /// <summary>
    /// Annule la demande de suppression et ferme la pop-up.
    /// </summary>
    public void CancelDelete()
    {
        CloseDeleteConfirmation();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Récupère les références des CoreSystems si elles n'ont pas encore été chargées.
    /// </summary>
    private void InitializeManagers()
    {
        if (saveManager == null) saveManager = CoreManager.Instance.GetComponent<SaveManager>();
        if (transitionManager == null) transitionManager = CoreManager.Instance.GetComponent<TransitionManager>();
    }

    /// <summary>
    /// Met à jour l'aspect visuel de tous les emplacements (textes, boutons) selon les données du disque,
    /// puis recalcule les chemins de navigation manette.
    /// </summary>
    private void RefreshAllSlots()
    {
        for (int i = 0; i < saveSlots.Length; i++)
        {
            GameData data = saveManager.GetSaveDataPreview(i);
            saveSlots[i].SetupSlot(i, data, this);
        }
        ConfigureNavigation();
    }

    /// <summary>
    /// Calcule et assigne mathématiquement les liaisons de navigation (Haut/Bas/Gauche/Droite) 
    /// pour chaque bouton, en incluant le bouton Retour et les corbeilles (si visibles).
    /// </summary>
    private void ConfigureNavigation()
    {
        for (int i = 0; i < saveSlots.Length; i++)
        {
            Button mainBtn = saveSlots[i].SlotButton;
            Button delBtn = saveSlots[i].DeleteButton;

            // --- CONFIGURATION DU SLOT PRINCIPAL ---
            Navigation mainNav = mainBtn.navigation;
            mainNav.mode = Navigation.Mode.Explicit;

            // Haut : Si 1er slot, on remonte vers Retour. Sinon, on va au slot du dessus.
            mainNav.selectOnUp = (i == 0) ? backButton : saveSlots[i - 1].SlotButton;

            // Bas : Si dernier slot, on descend vers Retour. Sinon, on va au slot du dessous.
            mainNav.selectOnDown = (i == saveSlots.Length - 1) ? backButton : saveSlots[i + 1].SlotButton;

            // Droite : Vers la corbeille (uniquement si elle est affichée car la sauvegarde existe)
            mainNav.selectOnRight = delBtn.gameObject.activeInHierarchy ? delBtn : null;
            mainNav.selectOnLeft = null;

            mainBtn.navigation = mainNav;

            // --- CONFIGURATION DE LA CORBEILLE ---
            if (delBtn.gameObject.activeInHierarchy)
            {
                Navigation delNav = delBtn.navigation;
                delNav.mode = Navigation.Mode.Explicit;

                delNav.selectOnLeft = mainBtn; // Retour vers le slot principal
                delNav.selectOnRight = null;

                // Calque les comportements Haut/Bas de la corbeille sur ceux du slot parent
                delNav.selectOnUp = mainNav.selectOnUp;
                delNav.selectOnDown = mainNav.selectOnDown;

                delBtn.navigation = delNav;
            }
        }

        // --- CONFIGURATION DU BOUTON RETOUR ---
        if (backButton != null)
        {
            Navigation backNav = backButton.navigation;
            backNav.mode = Navigation.Mode.Explicit;

            backNav.selectOnUp = saveSlots[saveSlots.Length - 1].SlotButton;
            backNav.selectOnDown = saveSlots[0].SlotButton;

            backNav.selectOnLeft = null;
            backNav.selectOnRight = null;
            backButton.navigation = backNav;
        }
    }

    /// <summary>
    /// Force la navigation de la pop-up de suppression à boucler uniquement entre "Oui" et "Non"
    /// pour empêcher le curseur de s'échapper (UI Bleeding).
    /// </summary>
    private void LockPopupNavigation()
    {
        Navigation confNav = confirmDeleteButton.navigation;
        confNav.mode = Navigation.Mode.Explicit;
        confNav.selectOnLeft = cancelDeleteButton;
        confNav.selectOnRight = cancelDeleteButton;
        confNav.selectOnUp = null;
        confNav.selectOnDown = null;
        confirmDeleteButton.navigation = confNav;

        Navigation cancNav = cancelDeleteButton.navigation;
        cancNav.mode = Navigation.Mode.Explicit;
        cancNav.selectOnLeft = confirmDeleteButton;
        cancNav.selectOnRight = confirmDeleteButton;
        cancNav.selectOnUp = null;
        cancNav.selectOnDown = null;
        cancelDeleteButton.navigation = cancNav;
    }

    /// <summary>
    /// Gère la fermeture visuelle de la pop-up de suppression, réactive l'arrière-plan, 
    /// et restaure le focus sur l'élément sélectionné précédemment.
    /// </summary>
    private void CloseDeleteConfirmation()
    {
        deleteConfirmationPanel.SetActive(false);
        pendingDeleteSlotIndex = -1;

        // Réactive l'interactivité des slots
        if (slotsContainerGroup != null) slotsContainerGroup.interactable = true;

        EventSystem.current.SetSelectedGameObject(null);
        if (previousFocusBeforeDelete != null && previousFocusBeforeDelete.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(previousFocusBeforeDelete);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(saveSlots[0].gameObject);
        }
    }
    #endregion
}