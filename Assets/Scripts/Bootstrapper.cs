using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

/// <summary>
/// Gère le démarrage du jeu. Joue une cinématique d'introduction, initialise le CoreManager,
/// charge le menu principal en arrière-plan et gère la logique pour passer la vidéo.
/// </summary>
public class Bootstrapper : MonoBehaviour
{
    #region Internal State
    [Header("Configuration")]
    [SerializeField]
    [Tooltip("Le nom de la scène du menu principal à charger.")]
    private string mainMenuSceneName = "MainMenu";

    [SerializeField]
    [Tooltip("Le lecteur vidéo jouant la cinématique de démarrage.")]
    private VideoPlayer videoPlayer;

    [SerializeField]
    [Tooltip("Le composant texte affichant l'invite pour passer la cinématique.")]
    private TextMeshProUGUI skipText;

    [Header("Transition")]
    [SerializeField]
    [Tooltip("Le Canvas Group utilisé pour le fondu au noir.")]
    private CanvasGroup fadePanel;

    [SerializeField]
    [Tooltip("La durée en secondes du fondu au noir.")]
    private float fadeDuration = 1f;

    [Header("Input")]
    [SerializeField]
    [Tooltip("L'action de l'Input System utilisée pour passer la vidéo.")]
    private InputActionReference skipAction;

    private AsyncOperation menuLoadOperation;
    private bool isSceneLoaded = false;
    private bool hasPressedOnce = false;
    private bool isTransitioning = false;
    #endregion

    #region Unity Life Cycle
    private void OnEnable()
    {
        if (skipAction != null)
        {
            skipAction.action.Enable();
            skipAction.action.performed += HandleInput;
        }
    }

    private void OnDisable()
    {
        if (skipAction != null)
        {
            skipAction.action.performed -= HandleInput;
            skipAction.action.Disable();
        }
    }

    private void Start()
    {
        skipText.alpha = 0f;
        if (fadePanel != null) fadePanel.alpha = 0f;

        CoreManager.Instance.Initialize();

        videoPlayer.loopPointReached += OnVideoFinished;

        videoPlayer.prepareCompleted += (vp) =>
        {
            vp.Play();
            StartCoroutine(LoadMenuAsync());
        };

        videoPlayer.Prepare();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Charge la scène du menu principal de manière asynchrone pendant que la vidéo joue.
    /// Met le jeu en attente jusqu'à ce que le CoreManager soit complètement initialisé.
    /// </summary>
    /// <returns>IEnumerator pour la coroutine.</returns>
    private IEnumerator LoadMenuAsync()
    {
        menuLoadOperation = SceneManager.LoadSceneAsync(mainMenuSceneName);
        menuLoadOperation.allowSceneActivation = false;

        yield return new WaitUntil(() => menuLoadOperation.progress >= 0.9f && CoreManager.Instance.IsInitialized);

        isSceneLoaded = true;

        if (hasPressedOnce)
        {
            skipText.text = "Appuyez à nouveau pour passer";
        }
    }

    /// <summary>
    /// Intercepte l'entrée du joueur. Gère la logique de double pression pour passer la cinématique,
    /// ou affiche l'état du chargement si la scène n'est pas encore prête.
    /// </summary>
    /// <param name="context">Le contexte de l'action fourni par le nouveau système d'inputs d'Unity.</param>
    private void HandleInput(InputAction.CallbackContext context)
    {
        if (isTransitioning) return;

        if (!hasPressedOnce)
        {
            hasPressedOnce = true;
            skipText.alpha = 1f;

            if (isSceneLoaded)
                skipText.text = "Appuyez à nouveau pour passer";
            else
                skipText.text = "Chargement en cours...";
        }
        else
        {
            if (isSceneLoaded)
            {
                StartCoroutine(FadeAndGoToMenu());
            }
        }
    }

    /// <summary>
    /// Callback appelé automatiquement lorsque la vidéo atteint sa fin.
    /// Lance la transition ou met en attente si le chargement n'est pas encore terminé.
    /// </summary>
    /// <param name="vp">L'instance du VideoPlayer ayant terminé sa lecture.</param>
    private void OnVideoFinished(VideoPlayer vp)
    {
        if (isTransitioning) return;

        if (isSceneLoaded)
        {
            StartCoroutine(FadeAndGoToMenu());
        }
        else
        {
            hasPressedOnce = true;
            skipText.alpha = 1f;
            skipText.text = "Chargement en cours...";
            StartCoroutine(WaitForLoadToFinish());
        }
    }

    /// <summary>
    /// Coroutine qui met le système en pause jusqu'à ce que la scène du menu soit chargée en arrière-plan.
    /// Déclenche la transition vers le menu dès que le chargement est achevé.
    /// </summary>
    /// <returns>IEnumerator pour la coroutine.</returns>
    private IEnumerator WaitForLoadToFinish()
    {
        yield return new WaitUntil(() => isSceneLoaded);
        if (!isTransitioning) StartCoroutine(FadeAndGoToMenu());
    }

    /// <summary>
    /// Gère l'animation de fondu au noir et autorise l'activation de la nouvelle scène.
    /// </summary>
    /// <returns>IEnumerator pour la coroutine de fondu.</returns>
    private IEnumerator FadeAndGoToMenu()
    {
        isTransitioning = true;
        skipText.alpha = 0f;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (fadePanel != null) fadePanel.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        menuLoadOperation.allowSceneActivation = true;
    }
    #endregion
}