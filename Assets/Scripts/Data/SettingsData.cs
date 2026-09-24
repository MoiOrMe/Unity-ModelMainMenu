using UnityEngine;

/// <summary>
/// Structure de données contenant tous les paramètres globaux du jeu (Graphismes, Audio, Contrôles).
/// Cette classe est conçue pour être sérialisée (ex: au format JSON) et sauvegardée de manière persistante sur le disque.
/// </summary>
[System.Serializable]
public class SettingsData
{
    #region Graphics Settings
    /// <summary>
    /// Largeur de la résolution de l'écran en pixels.
    /// </summary>
    public int resolutionWidth = 1920;

    /// <summary>
    /// Hauteur de la résolution de l'écran en pixels.
    /// </summary>
    public int resolutionHeight = 1080;

    /// <summary>
    /// Détermine si le jeu s'affiche en plein écran (true) ou en mode fenêtré (false).
    /// </summary>
    public bool isFullScreen = true;

    /// <summary>
    /// Index du niveau de qualité graphique globale (correspond aux paliers définis dans Edit > Project Settings > Quality).
    /// </summary>
    public int qualityLevel = 2;

    /// <summary>
    /// Détermine si le rendu des ombres en temps réel est activé.
    /// </summary>
    public bool enableShadows = true;
    #endregion

    #region Audio Settings
    /// <summary>
    /// Volume général de tout le jeu (Échelle linéaire de 0.0 à 1.0).
    /// </summary>
    public float masterVolume = 1f;

    /// <summary>
    /// Volume des pistes musicales (Échelle linéaire de 0.0 à 1.0).
    /// </summary>
    public float musicVolume = 1f;

    /// <summary>
    /// Volume des effets sonores et bruitages / SFX (Échelle linéaire de 0.0 à 1.0).
    /// </summary>
    public float sfxVolume = 1f;

    /// <summary>
    /// Volume des voix et des dialogues (Échelle linéaire de 0.0 à 1.0).
    /// </summary>
    public float dialogueVolume = 1f;

    /// <summary>
    /// Volume dédié aux scènes cinématiques (Échelle linéaire de 0.0 à 1.0).
    /// </summary>
    public float cinematicVolume = 1f;
    #endregion

    #region Input Settings
    /// <summary>
    /// Chaine de caractères contenant la représentation JSON des surcharges de touches (rebinds) personnalisées par le joueur.
    /// </summary>
    public string inputBindings = "";
    #endregion
}