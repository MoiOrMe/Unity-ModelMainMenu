using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Gère le défilement automatique d'un composant ScrollRect pour s'assurer que l'élément d'interface 
/// actuellement sélectionné (via clavier ou manette) reste toujours visible dans la zone d'affichage (Viewport).
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class AutoScrollToSelection : MonoBehaviour
{
    #region Internal State
    private ScrollRect scrollRect;
    private RectTransform viewport;
    private RectTransform content;

    /// <summary>
    /// Mémorise le dernier élément UI sélectionné pour éviter de recalculer inutilement le défilement à chaque frame.
    /// </summary>
    private GameObject lastSelected;
    #endregion

    #region Unity Life Cycle
    /// <summary>
    /// Initialise les références des composants nécessaires au calcul du défilement.
    /// </summary>
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        // Si le ScrollRect n'a pas de Viewport défini explicitement, on considère que c'est le composant lui-même
        viewport = scrollRect.viewport != null ? scrollRect.viewport : GetComponent<RectTransform>();
        content = scrollRect.content;
    }

    /// <summary>
    /// Vérifie si la sélection de l'EventSystem a changé. Si le nouvel élément fait partie du contenu 
    /// du ScrollRect, calcule sa position relative et ajuste la barre de défilement pour le centrer.
    /// </summary>
    private void Update()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        // On ignore si rien n'est sélectionné, si c'est le même objet qu'à la frame précédente, 
        // ou si l'objet sélectionné n'est pas un enfant de notre ScrollRect.
        if (selected == null || selected == lastSelected || !selected.transform.IsChildOf(content))
            return;

        lastSelected = selected;
        RectTransform selectedRect = selected.GetComponent<RectTransform>();

        // Force la mise à jour des Canvas pour s'assurer que les calculs de layout (comme un VerticalLayoutGroup) sont terminés
        Canvas.ForceUpdateCanvases();

        // Calcule la position locale de l'élément sélectionné par rapport au conteneur global
        Vector2 targetPos = (Vector2)content.InverseTransformPoint(selectedRect.position);

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        // Le défilement n'est nécessaire que si le contenu est plus grand que la zone visible
        if (contentHeight > viewportHeight)
        {
            // Calcule le décalage (offset) nécessaire pour centrer l'élément verticalement
            float offset = (targetPos.y * -1f) - (viewportHeight / 2f);
            float maxOffset = contentHeight - viewportHeight;

            // Convertit ce décalage en une valeur normalisée de 0 à 1 (format attendu par le ScrollRect)
            float normalizedPos = 1f - Mathf.Clamp01(offset / maxOffset);

            scrollRect.verticalNormalizedPosition = normalizedPos;
        }
    }
    #endregion
}