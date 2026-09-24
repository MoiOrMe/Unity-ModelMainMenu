using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gère l'affichage et les interactions d'un emplacement de sauvegarde individuel.
/// Fait le lien entre les données de progression (GameData) et les éléments visuels (textes, boutons),
/// tout en déléguant la logique métier (clic) au gestionnaire parent (SaveSlotsMenu).
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    #region Internal State
    [Header("UI Elements")]
    [SerializeField]
    [Tooltip("Le texte principal indiquant le numéro de sauvegarde ou 'Nouvelle Partie'.")]
    private TextMeshProUGUI statusText;

    [SerializeField]
    [Tooltip("Le texte affichant la date de la dernière sauvegarde ou 'Emplacement vide'.")]
    private TextMeshProUGUI dateText;

    [SerializeField]
    [Tooltip("Le texte affichant les détails de la progression (Niveau actuel, temps de jeu cumulé).")]
    private TextMeshProUGUI detailsText;

    [SerializeField]
    [Tooltip("Le bouton (corbeille) permettant de supprimer cette sauvegarde.")]
    private Button deleteButton;

    /// <summary>
    /// Référence publique vers le composant Button principal du slot.
    /// Utilisée par le SaveSlotsMenu pour configurer la navigation à la manette.
    /// </summary>
    public Button SlotButton => GetComponent<Button>();

    /// <summary>
    /// Référence publique vers le composant Button de suppression (corbeille).
    /// Utilisée par le SaveSlotsMenu pour configurer la navigation à la manette.
    /// </summary>
    public Button DeleteButton => deleteButton;

    private int slotIndex;
    private bool isEmpty;
    private GameData slotData;
    private SaveSlotsMenu parentMenu;
    #endregion

    #region Public Methods
    /// <summary>
    /// Initialise visuellement l'emplacement avec les données de sauvegarde fournies.
    /// Masque la corbeille si le slot est vide et formate joliment le temps de jeu.
    /// </summary>
    /// <param name="index">L'index de ce slot (ex: 0, 1, 2).</param>
    /// <param name="data">Les données de la sauvegarde lues sur le disque (peut être null si vide).</param>
    /// <param name="menu">La référence au menu parent pour y relayer les événements de clics.</param>
    public void SetupSlot(int index, GameData data, SaveSlotsMenu menu)
    {
        slotIndex = index;
        slotData = data;
        parentMenu = menu;
        isEmpty = (data == null);

        if (isEmpty)
        {
            statusText.text = $"Nouvelle Partie";
            dateText.text = "Emplacement vide";
            detailsText.text = "";

            // On désactive la corbeille puisqu'il n'y a rien à supprimer
            deleteButton.gameObject.SetActive(false);
        }
        else
        {
            statusText.text = $"Sauvegarde {index + 1}";
            dateText.text = data.lastSavedDate;

            // Conversion des secondes en format lisible (Heures, Minutes, Secondes)
            System.TimeSpan t = System.TimeSpan.FromSeconds(data.playTimeSeconds);
            detailsText.text = $"{data.currentLevelName} - {t.Hours}h {t.Minutes:D2}m {t.Seconds:D2}s";

            deleteButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Callback appelé par l'UI (événement OnClick de l'inspecteur) 
    /// lorsque le joueur clique sur le bouton principal du slot.
    /// </summary>
    public void ClickSlot()
    {
        parentMenu.OnSlotClicked(slotIndex, isEmpty, slotData);
    }

    /// <summary>
    /// Callback appelé par l'UI (événement OnClick de l'inspecteur) 
    /// lorsque le joueur clique sur l'icône de corbeille.
    /// </summary>
    public void ClickDelete()
    {
        parentMenu.OnDeleteClicked(slotIndex);
    }
    #endregion
}