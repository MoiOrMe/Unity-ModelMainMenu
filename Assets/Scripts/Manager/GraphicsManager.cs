using UnityEngine;

/// <summary>
/// Gère les paramètres d'affichage et graphiques du jeu (Résolution, Qualité, Ombres).
/// Applique les paramètres sauvegardés au démarrage et permet leur modification en cours de jeu.
/// </summary>
[RequireComponent(typeof(SaveManager))]
public class GraphicsManager : CoreSystem
{
    #region Internal State
    private SaveManager saveManager;
    #endregion

    #region Public Methods
    /// <summary>
    /// Initialise le gestionnaire graphique, récupère les données de sauvegarde 
    /// et applique les paramètres visuels actuels du joueur.
    /// </summary>
    public override void Initialize()
    {
        saveManager = GetComponent<SaveManager>();
        ApplySavedGraphics();
        IsReady = true;
    }

    /// <summary>
    /// Modifie la résolution de l'écran et sauvegarde le nouveau paramètre.
    /// </summary>
    /// <param name="width">La largeur de l'écran en pixels.</param>
    /// <param name="height">La hauteur de l'écran en pixels.</param>
    /// <param name="fullScreen">Vrai si le jeu doit s'afficher en plein écran, Faux pour le mode fenêtré.</param>
    public void SetResolution(int width, int height, bool fullScreen)
    {
        Screen.SetResolution(width, height, fullScreen);
        saveManager.CurrentSettings.resolutionWidth = width;
        saveManager.CurrentSettings.resolutionHeight = height;
        saveManager.CurrentSettings.isFullScreen = fullScreen;
        saveManager.SaveSettings();
    }

    /// <summary>
    /// Change le niveau de qualité global du jeu et sauvegarde ce paramètre.
    /// </summary>
    /// <param name="qualityIndex">L'index du niveau de qualité (défini dans Project Settings > Quality).</param>
    public void SetQuality(int qualityIndex)
    {
        saveManager.CurrentSettings.qualityLevel = qualityIndex;
        ApplyQualityAndShadows(qualityIndex, saveManager.CurrentSettings.enableShadows);
        saveManager.SaveSettings();
    }

    /// <summary>
    /// Active ou désactive le rendu des ombres dans le jeu et sauvegarde le choix.
    /// </summary>
    /// <param name="enabled">Vrai pour afficher les ombres, Faux pour les désactiver.</param>
    public void SetShadows(bool enabled)
    {
        saveManager.CurrentSettings.enableShadows = enabled;
        ApplyQualityAndShadows(saveManager.CurrentSettings.qualityLevel, enabled);
        saveManager.SaveSettings();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Lit les paramètres graphiques depuis les données de sauvegarde et les applique au moteur.
    /// </summary>
    private void ApplySavedGraphics()
    {
        SettingsData data = saveManager.CurrentSettings;

        Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, data.isFullScreen);
        ApplyQualityAndShadows(data.qualityLevel, data.enableShadows);
    }

    /// <summary>
    /// Applique conjointement le niveau de qualité global et l'état des ombres.
    /// </summary>
    /// <param name="qualityIndex">L'index de qualité globale à appliquer.</param>
    /// <param name="shadowsOn">Détermine si les ombres doivent être rendues ou non.</param>
    private void ApplyQualityAndShadows(int qualityIndex, bool shadowsOn)
    {
        // Applique le niveau de qualité global d'Unity (Project Settings > Quality)
        QualitySettings.SetQualityLevel(qualityIndex, true);

        QualitySettings.shadows = shadowsOn ? ShadowQuality.All : ShadowQuality.Disable;
    }
    #endregion
}