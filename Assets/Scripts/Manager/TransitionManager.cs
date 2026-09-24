using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Gère les transitions fluides (fondu au noir) entre les différentes scènes du jeu.
/// S'occupe du chargement asynchrone et met à jour dynamiquement le contexte des contrôles (InputMap) selon la scène chargée.
/// </summary>
public class TransitionManager : CoreSystem
{
    #region Internal State
    [Header("UI Transition")]
    [SerializeField]
    [Tooltip("Le Canvas Group utilisé pour appliquer le fondu (alpha) et bloquer les clics lors de la transition.")]
    private CanvasGroup fadeCanvasGroup;

    [SerializeField]
    [Tooltip("Le texte affiché pendant le chargement de la scène en arrière-plan.")]
    private TextMeshProUGUI loadingText;

    [Header("Paramètres")]
    [SerializeField]
    [Tooltip("La durée en secondes du fondu (vers le noir, puis vers la nouvelle scène).")]
    private float fadeDuration = 1f;
    #endregion

    #region Public Methods
    /// <summary>
    /// Initialise le gestionnaire de transition, s'assure que l'écran de chargement est transparent
    /// et non bloquant, puis marque le système comme prêt.
    /// </summary>
    public override void Initialize()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(false);
        }

        IsReady = true;
        Debug.Log("TransitionManager : Prêt.");
    }

    /// <summary>
    /// Lance le processus de transition vers une nouvelle scène avec un écran de chargement.
    /// </summary>
    /// <param name="sceneName">Le nom exact de la scène à charger (doit être configurée dans les Build Settings).</param>
    public void LoadLevel(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Coroutine gérant la séquence complète : fondu au noir, chargement asynchrone en tâche de fond,
    /// bascule du mode des inputs, puis révélation de la nouvelle scène.
    /// </summary>
    /// <param name="sceneName">Le nom de la scène à charger.</param>
    /// <returns>IEnumerator pour l'exécution de la coroutine.</returns>
    private IEnumerator TransitionRoutine(string sceneName)
    {
        // Bloque les interactions UI pendant la transition (évite de spammer les boutons)
        fadeCanvasGroup.blocksRaycasts = true;

        // Fondu au noir (Fade Out)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        if (loadingText != null) loadingText.gameObject.SetActive(true);

        // Chargement asynchrone de la scène
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // Petite pause pour garantir un affichage fluide, même sur les machines très rapides
        yield return new WaitForSeconds(0.2f);

        operation.allowSceneActivation = true;

        yield return new WaitUntil(() => operation.isDone);

        // Bascule des inputs intelligente
        if (sceneName == "MainMenu")
        {
            CoreManager.Instance.GetComponent<InputManager>().EnableUIMap();
        }
        else
        {
            CoreManager.Instance.GetComponent<InputManager>().EnableGameplayMap();
        }

        if (loadingText != null) loadingText.gameObject.SetActive(false);

        // Fondu vers la nouvelle scène (Fade In)
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        // Débloque les interactions UI pour la nouvelle scène
        fadeCanvasGroup.blocksRaycasts = false;
    }
    #endregion
}