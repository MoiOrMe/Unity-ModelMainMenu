using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Gère les différents canaux sonores du jeu.
/// Fait le lien entre les paramètres sauvegardés du joueur et l'AudioMixer d'Unity, 
/// en gérant notamment la conversion des volumes linéaires en décibels logarithmiques.
/// </summary>
[RequireComponent(typeof(SaveManager))]
public class AudioManager : CoreSystem
{
    #region Internal State
    [Header("Configuration")]
    [Tooltip("L'AudioMixer principal du jeu qui contient tous les canaux exposés.")]
    [SerializeField]
    private AudioMixer mainMixer;

    private SaveManager saveManager;
    #endregion

    #region Public Methods
    /// <summary>
    /// Initialise le système audio, récupère les dépendances et applique les volumes de la dernière sauvegarde.
    /// </summary>
    public override void Initialize()
    {
        saveManager = GetComponent<SaveManager>();

        if (mainMixer == null)
        {
            Debug.LogWarning("AudioManager : Aucun AudioMixer n'a été assigné dans l'inspecteur !");
            IsReady = true;
            return;
        }

        ApplySavedAudio();

        IsReady = true;
        Debug.Log("AudioManager : Volumes appliqués.");
    }

    /// <summary>
    /// Modifie le volume d'un canal spécifique de l'AudioMixer et met à jour les données de sauvegarde.
    /// Convertit automatiquement la valeur linéaire en échelle logarithmique (décibels).
    /// </summary>
    /// <param name="exposedParameter">Le nom exact du paramètre exposé dans l'AudioMixer (ex: "MasterVolume").</param>
    /// <param name="linearValue">La valeur de volume désirée, sur une échelle linéaire de 0.0 à 1.0.</param>
    /// <param name="saveImmediately">Si vrai, sauvegarde directement les paramètres sur le disque. Faux par défaut lors du chargement initial pour éviter les surécritures inutiles.</param>
    public void SetVolume(string exposedParameter, float linearValue, bool saveImmediately = true)
    {
        // L'AudioMixer fonctionne en décibels (échelle logarithmique). 
        // On convertit la valeur linéaire (0 à 1) en décibels (-80dB à 0dB).
        // Le Clamp évite l'erreur mathématique Log10(0) qui renverrait l'infini.
        float clampedValue = Mathf.Clamp(linearValue, 0.0001f, 1f);
        float decibelValue = Mathf.Log10(clampedValue) * 20f;

        mainMixer.SetFloat(exposedParameter, decibelValue);

        // Enregistrement de la nouvelle valeur dans la classe de données en mémoire
        if (exposedParameter == "MasterVolume") saveManager.CurrentSettings.masterVolume = linearValue;
        else if (exposedParameter == "MusicVolume") saveManager.CurrentSettings.musicVolume = linearValue;
        else if (exposedParameter == "SFXVolume") saveManager.CurrentSettings.sfxVolume = linearValue;
        else if (exposedParameter == "DialogueVolume") saveManager.CurrentSettings.dialogueVolume = linearValue;
        else if (exposedParameter == "CinematicVolume") saveManager.CurrentSettings.cinematicVolume = linearValue;

        // Écriture sur le disque si demandé
        if (saveImmediately)
        {
            saveManager.SaveSettings();
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Récupère les données de configuration depuis le SaveManager et applique les volumes
    /// à tous les sous-canaux sans forcer une réécriture immédiate sur le disque.
    /// </summary>
    private void ApplySavedAudio()
    {
        SettingsData data = saveManager.CurrentSettings;

        SetVolume("MasterVolume", data.masterVolume, false);
        SetVolume("MusicVolume", data.musicVolume, false);
        SetVolume("SFXVolume", data.sfxVolume, false);
        SetVolume("DialogueVolume", data.dialogueVolume, false);
        SetVolume("CinematicVolume", data.cinematicVolume, false);
    }
    #endregion
}