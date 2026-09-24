using UnityEngine;
using System;

/// <summary>
/// Structure de données contenant toute la progression d'une partie (Game Data).
/// Cette classe est conçue pour être sérialisée (convertie en JSON) et sauvegardée par le SaveManager.
/// </summary>
[Serializable]
public class GameData
{
    #region Metadata (Informations Générales)
    /// <summary>
    /// L'index de l'emplacement de sauvegarde (ex: 0, 1, 2) correspondant à ce fichier.
    /// </summary>
    public int saveSlotIndex;

    /// <summary>
    /// Date et heure de la dernière sauvegarde, formatée en chaîne de caractères (ex: "25/10/2026 - 14:30").
    /// Utilisé pour l'affichage dans les menus.
    /// </summary>
    public string lastSavedDate;

    /// <summary>
    /// Le nom exact de la scène dans laquelle le joueur se trouvait lors de la sauvegarde.
    /// Utilisé pour recharger le bon niveau.
    /// </summary>
    public string currentLevelName;

    /// <summary>
    /// Le temps de jeu total cumulé sur cette sauvegarde, exprimé en secondes.
    /// </summary>
    public float playTimeSeconds;
    #endregion

    #region Gameplay Data (En attente d'implémentation)
    // --- SQUELETTE POUR LE FUTUR GAMEPLAY ---
    // Ces variables devront être décommentées et gérées par d'autres systèmes 
    // (comme un PlayerController ou un InventoryManager) au fil du développement.

    // public float playerPosX, playerPosY, playerPosZ;
    // public int playerHealth;
    // public List<string> unlockedItems;
    // public Dictionary<string, bool> questStates;
    #endregion

    #region Constructor
    /// <summary>
    /// Constructeur appelé lors de la création d'une toute nouvelle partie.
    /// Initialise les métadonnées de base et met le compteur de temps à zéro.
    /// </summary>
    /// <param name="slotIndex">L'index du slot (emplacement) choisi par le joueur.</param>
    /// <param name="startingLevel">Le nom de la scène (niveau) de départ du jeu.</param>
    public GameData(int slotIndex, string startingLevel)
    {
        this.saveSlotIndex = slotIndex;
        this.lastSavedDate = DateTime.Now.ToString("dd/MM/yyyy - HH:mm");
        this.currentLevelName = startingLevel;
        this.playTimeSeconds = 0f;
    }
    #endregion
}