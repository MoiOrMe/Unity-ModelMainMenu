using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Contrôleur gérant l'interface utilisateur des options du jeu.
/// Permet la navigation par onglets (Graphismes, Audio, Gameplay) et fait le lien 
/// entre les éléments d'UI (Sliders, Toggles, Dropdowns) et les sous-systèmes (CoreSystems).
/// </summary>
public class OptionsController : MonoBehaviour
{
    #region Internal State
    [Header("Système d'Onglets")]
    [SerializeField] private GameObject graphicsTab;
    [SerializeField] private GameObject audioTab;
    [SerializeField] private GameObject gameplayTab;

    [SerializeField] private GameObject firstGraphicsElement;
    [SerializeField] private GameObject firstAudioElement;
    [SerializeField] private GameObject firstGameplayElement;

    [Header("Input - Changement d'Onglet")]
    [SerializeField] private InputActionReference previousTabAction;
    [SerializeField] private InputActionReference nextTabAction;

    [Header("Visuel des Onglets")]
    [SerializeField] private Image graphicsTabImage;
    [SerializeField] private Image audioTabImage;
    [SerializeField] private Image gameplayTabImage;
    [SerializeField] private Color activeTabColor = new Color(0f, 0.64f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(1f, 1f, 1f);

    [Header("UI - Graphismes")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle shadowsToggle;

    [Header("UI - Audio")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider dialogueSlider;
    [SerializeField] private Slider cinematicSlider;

    private AudioManager audioManager;
    private GraphicsManager graphicsManager;
    private SaveManager saveManager;
    private Resolution[] availableResolutions;

    private int currentTabIndex = 0;
    #endregion

    #region Unity Life Cycle
    private void Start()
    {
        audioManager = CoreManager.Instance.GetComponent<AudioManager>();
        graphicsManager = CoreManager.Instance.GetComponent<GraphicsManager>();
        saveManager = CoreManager.Instance.GetComponent<SaveManager>();

        InitializeUI();
        OpenTabGraphics();
    }

    private void OnEnable()
    {
        if (previousTabAction != null) previousTabAction.action.performed += OnPreviousTab;
        if (nextTabAction != null) nextTabAction.action.performed += OnNextTab;
    }

    private void OnDisable()
    {
        if (previousTabAction != null) previousTabAction.action.performed -= OnPreviousTab;
        if (nextTabAction != null) nextTabAction.action.performed -= OnNextTab;
    }
    #endregion

    #region Public Methods (UI Callbacks)
    /// <summary>
    /// Ouvre l'onglet des graphismes et place le focus sur son premier élément.
    /// </summary>
    public void OpenTabGraphics()
    {
        currentTabIndex = 0;
        SwitchTab(graphicsTab, firstGraphicsElement);
    }

    /// <summary>
    /// Ouvre l'onglet de l'audio et place le focus sur son premier élément.
    /// </summary>
    public void OpenTabAudio()
    {
        currentTabIndex = 1;
        SwitchTab(audioTab, firstAudioElement);
    }

    /// <summary>
    /// Ouvre l'onglet de gameplay et place le focus sur son premier élément.
    /// </summary>
    public void OpenTabGameplay()
    {
        currentTabIndex = 2;
        SwitchTab(gameplayTab, firstGameplayElement);
    }

    /// <summary>
    /// Callback appelé par le Slider Master. Met à jour le volume global.
    /// </summary>
    /// <param name="value">La valeur du volume (entre 0 et 1).</param>
    public void SetMasterVolume(float value) => audioManager.SetVolume("MasterVolume", value);

    /// <summary>
    /// Callback appelé par le Slider Musique. Met à jour le volume de la musique.
    /// </summary>
    /// <param name="value">La valeur du volume (entre 0 et 1).</param>
    public void SetMusicVolume(float value) => audioManager.SetVolume("MusicVolume", value);

    /// <summary>
    /// Callback appelé par le Slider SFX. Met à jour le volume des bruitages.
    /// </summary>
    /// <param name="value">La valeur du volume (entre 0 et 1).</param>
    public void SetSFXVolume(float value) => audioManager.SetVolume("SFXVolume", value);

    /// <summary>
    /// Callback appelé par le Slider Dialogue. Met à jour le volume des dialogues.
    /// </summary>
    /// <param name="value">La valeur du volume (entre 0 et 1).</param>
    public void SetDialogueVolume(float value) => audioManager.SetVolume("DialogueVolume", value);

    /// <summary>
    /// Callback appelé par le Slider Cinématiques. Met à jour le volume des cinématiques.
    /// </summary>
    /// <param name="value">La valeur du volume (entre 0 et 1).</param>
    public void SetCinematicVolume(float value) => audioManager.SetVolume("CinematicVolume", value);

    /// <summary>
    /// Callback appelé par le Toggle Plein Écran.
    /// </summary>
    /// <param name="isFullscreen">L'état désiré du plein écran.</param>
    public void SetFullscreen(bool isFullscreen) => graphicsManager.SetResolution(saveManager.CurrentSettings.resolutionWidth, saveManager.CurrentSettings.resolutionHeight, isFullscreen);

    /// <summary>
    /// Callback appelé par le Dropdown de résolution.
    /// </summary>
    /// <param name="index">L'index de la résolution choisie dans le menu déroulant.</param>
    public void SetResolution(int index) => graphicsManager.SetResolution(availableResolutions[index].width, availableResolutions[index].height, saveManager.CurrentSettings.isFullScreen);

    /// <summary>
    /// Callback appelé par le Dropdown de qualité.
    /// </summary>
    /// <param name="index">L'index de la qualité globale (défini dans Project Settings > Quality).</param>
    public void SetQuality(int index) => graphicsManager.SetQuality(index);

    /// <summary>
    /// Callback appelé par le Toggle des ombres.
    /// </summary>
    /// <param name="isOn">L'état désiré des ombres.</param>
    public void SetShadows(bool isOn) => graphicsManager.SetShadows(isOn);
    #endregion

    #region Private Methods
    /// <summary>
    /// Méthode d'abonnement au raccourci clavier/manette pour reculer d'un onglet.
    /// </summary>
    /// <param name="ctx">Le contexte de l'input.</param>
    private void OnPreviousTab(InputAction.CallbackContext ctx) => CycleTab(-1);

    /// <summary>
    /// Méthode d'abonnement au raccourci clavier/manette pour avancer d'un onglet.
    /// </summary>
    /// <param name="ctx">Le contexte de l'input.</param>
    private void OnNextTab(InputAction.CallbackContext ctx) => CycleTab(1);

    /// <summary>
    /// Gère la boucle de navigation entre les onglets existants.
    /// </summary>
    /// <param name="direction">-1 pour l'onglet précédent, 1 pour l'onglet suivant.</param>
    private void CycleTab(int direction)
    {
        currentTabIndex += direction;

        if (currentTabIndex > 2) currentTabIndex = 0;
        if (currentTabIndex < 0) currentTabIndex = 2;

        if (currentTabIndex == 0) OpenTabGraphics();
        else if (currentTabIndex == 1) OpenTabAudio();
        else if (currentTabIndex == 2) OpenTabGameplay();
    }

    /// <summary>
    /// Cache tous les onglets, affiche celui demandé, applique la coloration des boutons
    /// et donne le focus à un élément spécifique pour la navigation manette/clavier.
    /// </summary>
    /// <param name="tabToOpen">Le GameObject contenant l'onglet à afficher.</param>
    /// <param name="firstElementToFocus">Le GameObject du premier bouton ou slider à sélectionner dans cet onglet.</param>
    private void SwitchTab(GameObject tabToOpen, GameObject firstElementToFocus)
    {
        graphicsTab.SetActive(false);
        audioTab.SetActive(false);
        gameplayTab.SetActive(false);
        tabToOpen.SetActive(true);

        if (graphicsTabImage != null) graphicsTabImage.color = (tabToOpen == graphicsTab) ? activeTabColor : inactiveTabColor;
        if (audioTabImage != null) audioTabImage.color = (tabToOpen == audioTab) ? activeTabColor : inactiveTabColor;
        if (gameplayTabImage != null) gameplayTabImage.color = (tabToOpen == gameplayTab) ? activeTabColor : inactiveTabColor;

        if (firstElementToFocus != null && firstElementToFocus.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(firstElementToFocus);
        }
    }

    /// <summary>
    /// Initialise tous les contrôles de l'interface en utilisant les données de configuration actuelles.
    /// S'assure de lier les callbacks sans déclencher d'événements superflus à l'initialisation.
    /// </summary>
    private void InitializeUI()
    {
        SettingsData data = saveManager.CurrentSettings;

        SetupSlider(masterSlider, data.masterVolume, SetMasterVolume);
        SetupSlider(musicSlider, data.musicVolume, SetMusicVolume);
        SetupSlider(sfxSlider, data.sfxVolume, SetSFXVolume);
        SetupSlider(dialogueSlider, data.dialogueVolume, SetDialogueVolume);
        SetupSlider(cinematicSlider, data.cinematicVolume, SetCinematicVolume);

        fullscreenToggle.onValueChanged.RemoveAllListeners();
        fullscreenToggle.isOn = data.isFullScreen;
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        shadowsToggle.onValueChanged.RemoveAllListeners();
        shadowsToggle.isOn = data.enableShadows;
        shadowsToggle.onValueChanged.AddListener(SetShadows);

        qualityDropdown.onValueChanged.RemoveAllListeners();
        qualityDropdown.value = data.qualityLevel;
        qualityDropdown.onValueChanged.AddListener(SetQuality);

        InitializeResolutionDropdown(data);
    }

    /// <summary>
    /// Configure un Slider avec sa valeur par défaut et lie la fonction à appeler lors de sa modification.
    /// </summary>
    /// <param name="slider">Le composant Slider à configurer.</param>
    /// <param name="savedValue">La valeur récupérée de la sauvegarde (0 à 1).</param>
    /// <param name="action">La méthode de callback à invoquer (ex: SetMasterVolume).</param>
    private void SetupSlider(Slider slider, float savedValue, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null) return;
        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.onValueChanged.RemoveAllListeners();
        slider.value = savedValue;
        slider.onValueChanged.AddListener(action);
    }

    /// <summary>
    /// Peuple le menu déroulant avec les résolutions d'écran supportées par la machine du joueur,
    /// et sélectionne la résolution courante lue depuis la sauvegarde.
    /// </summary>
    /// <param name="data">Les données de configuration actuelles.</param>
    private void InitializeResolutionDropdown(SettingsData data)
    {
        if (resolutionDropdown == null) return;
        resolutionDropdown.ClearOptions();

        availableResolutions = Screen.resolutions;
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            options.Add(availableResolutions[i].width + " x " + availableResolutions[i].height);
            if (availableResolutions[i].width == data.resolutionWidth && availableResolutions[i].height == data.resolutionHeight)
                currentResolutionIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }
    #endregion
}