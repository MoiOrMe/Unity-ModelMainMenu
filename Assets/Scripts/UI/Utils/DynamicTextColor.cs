using UnityEngine;
using TMPro;

/// <summary>
/// Analyse périodiquement une zone spécifique d'une RenderTexture (vidéo) pour adapter dynamiquement
/// la couleur d'un texte (clair ou sombre) afin de garantir sa lisibilité.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class DynamicTextColor : MonoBehaviour
{
    #region Internal State
    [Header("Configuration")]
    [Tooltip("La RenderTexture sur laquelle joue la vidéo")]
    public RenderTexture videoTexture;

    [Tooltip("Fréquence de mise à jour (en secondes). 0.2 = 5 fois par seconde")]
    public float updateRate = 0.2f;

    [Header("Couleurs")]
    [Tooltip("Couleur à appliquer si le fond est sombre.")]
    public Color lightTextColor = Color.white;

    [Tooltip("Couleur à appliquer si le fond est clair.")]
    public Color darkTextColor = Color.black;

    private TextMeshProUGUI textMesh;
    private Texture2D tempTexture;
    private float timer;
    #endregion

    #region Unity Life Cycle
    private void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();

        // Création d'une texture temporaire de 10x10 pixels pour l'échantillonnage
        tempTexture = new Texture2D(10, 10, TextureFormat.RGB24, false);
    }

    private void Update()
    {
        if (videoTexture == null) return;

        timer += Time.deltaTime;
        if (timer >= updateRate)
        {
            timer = 0;
            UpdateTextColor();
        }
    }

    private void OnDestroy()
    {
        if (tempTexture != null)
        {
            Destroy(tempTexture);
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Lit un échantillon de pixels sur la RenderTexture, calcule la luminance moyenne,
    /// et applique la couleur de texte appropriée (claire ou sombre) tout en conservant l'alpha actuel.
    /// </summary>
    private void UpdateTextColor()
    {
        // Sauvegarde de la RenderTexture active actuelle
        RenderTexture currentActive = RenderTexture.active;
        RenderTexture.active = videoTexture;

        // Définition de la zone de lecture (en bas à droite du texte, généralement)
        int startX = Mathf.FloorToInt(videoTexture.width * 0.75f);
        int startY = Mathf.FloorToInt(videoTexture.height * 0.1f);

        // Sécurité pour éviter de lire en dehors des limites de la texture
        startX = Mathf.Clamp(startX, 0, videoTexture.width - 10);
        startY = Mathf.Clamp(startY, 0, videoTexture.height - 10);

        // Lecture des pixels depuis le GPU vers le CPU
        Rect readRect = new Rect(startX, startY, 10, 10);
        tempTexture.ReadPixels(readRect, 0, 0);
        tempTexture.Apply();

        // Restauration de la RenderTexture active
        RenderTexture.active = currentActive;

        Color[] pixels = tempTexture.GetPixels();
        float totalLuminance = 0;

        // Calcul de la luminance selon la formule standard BT.601
        for (int i = 0; i < pixels.Length; i++)
        {
            float luminance = (0.299f * pixels[i].r) + (0.587f * pixels[i].g) + (0.114f * pixels[i].b);
            totalLuminance += luminance;
        }

        float averageLuminance = totalLuminance / pixels.Length;

        // Bascule vers le texte sombre si le fond est clair, et inversement
        Color targetColor = averageLuminance > 0.5f ? darkTextColor : lightTextColor;

        // Maintien de la transparence (alpha) gérée par d'autres scripts (ex: fondu)
        targetColor.a = textMesh.alpha;

        textMesh.color = targetColor;
    }
    #endregion
}