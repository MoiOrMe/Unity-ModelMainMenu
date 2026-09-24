using System.IO;
using UnityEngine;

/// <summary>
/// Gère la persistance des données du jeu sur le disque.
/// S'occupe à la fois des paramètres globaux (Settings) et des sauvegardes de progression multislots (GameData).
/// Assure la sérialisation en JSON et le chiffrement des fichiers pour éviter la triche ou la corruption.
/// </summary>
public class SaveManager : CoreSystem
{
    #region Internal State
    /// <summary>
    /// Les paramètres globaux actuels (Graphismes, Audio, Inputs) chargés en mémoire.
    /// </summary>
    public SettingsData CurrentSettings { get; private set; }

    /// <summary>
    /// Le chemin d'accès absolu vers le fichier de paramètres sur la machine du joueur.
    /// </summary>
    private string settingsFilePath;

    [Header("Game Data")]
    /// <summary>
    /// Les données de progression de la partie actuellement en cours. 
    /// Vaut null si le joueur est sur le menu principal sans partie chargée.
    /// </summary>
    private GameData currentGameData;

    /// <summary>
    /// L'index de l'emplacement de sauvegarde actuel (ex: 0, 1, 2). Vaut -1 si aucune partie n'est chargée.
    /// </summary>
    private int currentSlot = -1;

    public GameData CurrentGameData => currentGameData;
    public int CurrentSlot => currentSlot;
    #endregion

    #region Unity Life Cycle
    private void Update()
    {
        // Incrémente le temps de jeu actif uniquement si une partie est chargée
        if (currentGameData != null && currentSlot != -1)
        {
            currentGameData.playTimeSeconds += Time.deltaTime;
        }
    }

    private void OnApplicationQuit()
    {
        // Sauvegarde de sécurité absolue en cas de fermeture brutale via Alt+F4 ou la croix de la fenêtre
        if (currentGameData != null && currentSlot != -1)
        {
            SaveCurrentGame();
        }
        SaveSettings();
    }
    #endregion

    #region CoreSystem Implementation
    /// <summary>
    /// Initialise le gestionnaire, détermine les chemins d'accès locaux, charge les paramètres existants 
    /// (ou en crée de nouveaux) et signale au CoreManager qu'il est prêt.
    /// </summary>
    public override void Initialize()
    {
        // Chemin vers AppData/LocalLow/[NomDeLaCompagnie]/[NomDuJeu] (sous Windows)
        settingsFilePath = Path.Combine(Application.persistentDataPath, "settings.dat");

        CurrentSettings = LoadData<SettingsData>(settingsFilePath) ?? new SettingsData();

        SaveSettings();

        IsReady = true;
    }
    #endregion

    #region Public Methods - Core Data
    /// <summary>
    /// Force l'écriture des paramètres globaux actuels (CurrentSettings) sur le disque.
    /// </summary>
    public void SaveSettings()
    {
        SaveData(CurrentSettings, settingsFilePath);
    }

    /// <summary>
    /// Méthode générique pour sérialiser et chiffrer n'importe quel objet de données vers un fichier physique.
    /// </summary>
    /// <typeparam name="T">Le type de l'objet à sauvegarder.</typeparam>
    /// <param name="data">L'objet contenant les données.</param>
    /// <param name="path">Le chemin absolu du fichier de destination.</param>
    public void SaveData<T>(T data, string path)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            string encryptedJson = CryptoUtility.Encrypt(json);
            File.WriteAllText(path, encryptedJson);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveManager : Erreur lors de la sauvegarde : {e.Message}");
        }
    }

    /// <summary>
    /// Méthode générique pour lire, déchiffrer et désérialiser un fichier physique vers un objet C#.
    /// </summary>
    /// <typeparam name="T">Le type de l'objet attendu en retour.</typeparam>
    /// <param name="path">Le chemin absolu du fichier à lire.</param>
    /// <returns>L'objet désérialisé, ou null si le fichier n'existe pas ou est corrompu.</returns>
    public T LoadData<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;

        try
        {
            string encryptedJson = File.ReadAllText(path);
            string json = CryptoUtility.Decrypt(encryptedJson);
            return JsonUtility.FromJson<T>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveManager : Fichier potentiellement corrompu. Création d'un nouveau fichier. Erreur : {e.Message}");
            return null;
        }
    }
    #endregion

    #region Public Methods - Slots Management
    /// <summary>
    /// Vérifie si un fichier de sauvegarde existe pour un emplacement donné.
    /// </summary>
    /// <param name="slotIndex">Le numéro de l'emplacement (ex: 0, 1, 2).</param>
    /// <returns>Vrai si le fichier existe, Faux sinon.</returns>
    public bool DoesSaveExist(int slotIndex)
    {
        return File.Exists(GetSaveFilePath(slotIndex));
    }

    /// <summary>
    /// Lit les données d'une sauvegarde de manière isolée pour afficher un aperçu dans les menus
    /// sans écraser la partie potentiellement en cours en mémoire.
    /// </summary>
    /// <param name="slotIndex">L'index de l'emplacement à inspecter.</param>
    /// <returns>Un objet GameData contenant les infos de la sauvegarde, ou null en cas d'erreur.</returns>
    public GameData GetSaveDataPreview(int slotIndex)
    {
        string path = GetSaveFilePath(slotIndex);
        if (File.Exists(path))
        {
            try
            {
                string encryptedJson = File.ReadAllText(path);
                string decryptedJson = CryptoUtility.Decrypt(encryptedJson);
                return JsonUtility.FromJson<GameData>(decryptedJson);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Erreur de lecture du slot {slotIndex} : {e.Message}");
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Crée une toute nouvelle partie vierge dans l'emplacement spécifié et l'écrit sur le disque.
    /// </summary>
    /// <param name="slotIndex">L'index de l'emplacement choisi par le joueur.</param>
    /// <param name="startingLevel">Le nom de la scène de départ.</param>
    public void CreateNewGame(int slotIndex, string startingLevel)
    {
        currentSlot = slotIndex;
        currentGameData = new GameData(slotIndex, startingLevel);
        SaveCurrentGame();
    }

    /// <summary>
    /// Charge intégralement les données de progression d'un emplacement spécifique en mémoire centrale.
    /// </summary>
    /// <param name="slotIndex">L'index de la sauvegarde à charger.</param>
    public void LoadGame(int slotIndex)
    {
        if (DoesSaveExist(slotIndex))
        {
            currentSlot = slotIndex;
            currentGameData = GetSaveDataPreview(slotIndex);
            Debug.Log($"Partie chargée depuis le slot {slotIndex}");
        }
    }

    /// <summary>
    /// Enregistre la progression de la partie actuellement en mémoire sur le disque.
    /// Met automatiquement à jour la date de dernière sauvegarde.
    /// </summary>
    public void SaveCurrentGame()
    {
        if (currentGameData == null || currentSlot == -1) return;

        currentGameData.lastSavedDate = System.DateTime.Now.ToString("dd/MM/yyyy - HH:mm");

        string json = JsonUtility.ToJson(currentGameData, true);
        string encryptedJson = CryptoUtility.Encrypt(json);

        File.WriteAllText(GetSaveFilePath(currentSlot), encryptedJson);
        Debug.Log($"Jeu sauvegardé dans le slot {currentSlot}");
    }

    /// <summary>
    /// Supprime définitivement le fichier de sauvegarde d'un emplacement spécifique.
    /// Vide également la mémoire s'il s'agissait de la partie actuellement en cours.
    /// </summary>
    /// <param name="slotIndex">L'index de l'emplacement à effacer.</param>
    public void DeleteSave(int slotIndex)
    {
        string path = GetSaveFilePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);

            // Si on supprime la partie sur laquelle on était en train de jouer, on nettoie la mémoire
            if (currentSlot == slotIndex)
            {
                currentSlot = -1;
                currentGameData = null;
            }
        }
    }

    /// <summary>
    /// Clôture la session de jeu actuelle (ex: lors du retour au menu principal).
    /// Met à jour la scène active, sauvegarde une dernière fois, puis décharge les données de la mémoire.
    /// </summary>
    public void CloseCurrentSession()
    {
        if (currentGameData != null && currentSlot != -1)
        {
            // Met à jour la position de départ pour le prochain chargement
            currentGameData.currentLevelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            SaveCurrentGame();

            currentSlot = -1;
            currentGameData = null;
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Construit le chemin absolu vers un fichier de sauvegarde de progression spécifique.
    /// </summary>
    /// <param name="slotIndex">Le numéro de l'emplacement.</param>
    /// <returns>Une chaîne représentant le chemin complet du fichier.</returns>
    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"save_{slotIndex}.dat");
    }
    #endregion
}