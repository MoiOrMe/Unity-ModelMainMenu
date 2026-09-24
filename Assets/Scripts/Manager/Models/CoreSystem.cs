using UnityEngine;

/// <summary>
/// Classe de base abstraite pour tous les sous-systèmes (Managers) du jeu.
/// Garantit que chaque système possède un état de préparation et une méthode d'initialisation standardisée.
/// </summary>
public abstract class CoreSystem : MonoBehaviour
{
    #region Internal State
    /// <summary>
    /// Indique si le sous-système a terminé son initialisation et est prêt à être utilisé par le reste du jeu.
    /// </summary>
    public bool IsReady { get; protected set; } = false;
    #endregion

    #region Public Methods
    /// <summary>
    /// Méthode abstraite appelée par le CoreManager lors de la séquence de démarrage.
    /// Chaque sous-système enfant doit implémenter sa propre logique d'initialisation ici
    /// et passer IsReady à true une fois terminé.
    /// </summary>
    public abstract void Initialize();
    #endregion
}