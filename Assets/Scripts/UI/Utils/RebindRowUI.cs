using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Conteneur UI représentant une ligne individuelle dans le menu de modification des contrôles (Rebind).
/// Regroupe les références vers les textes et les boutons pour une action spécifique (ex: "Sauter").
/// </summary>
public class RebindRowUI : MonoBehaviour
{
    #region Internal State
    [Header("Texte de l'Action")]
    /// <summary>
    /// Le composant texte affichant le nom humainement lisible de l'action (ex: "Avancer", "Tirer").
    /// </summary>
    public TextMeshProUGUI actionNameText;

    [Header("Assignation Clavier/Souris")]
    /// <summary>
    /// Le composant bouton sur lequel le joueur clique pour remapper la touche clavier ou souris.
    /// </summary>
    public Button keyboardButton;

    /// <summary>
    /// Le texte à l'intérieur du bouton clavier affichant la touche actuellement assignée (ex: "Espace", "Clic Gauche").
    /// </summary>
    public TextMeshProUGUI keyboardButtonText;

    [Header("Assignation Manette")]
    /// <summary>
    /// Le composant bouton sur lequel le joueur clique pour remapper la touche de la manette.
    /// </summary>
    public Button gamepadButton;

    /// <summary>
    /// Le texte à l'intérieur du bouton manette affichant la touche actuellement assignée (ex: "Croix", "R2").
    /// </summary>
    public TextMeshProUGUI gamepadButtonText;
    #endregion
}