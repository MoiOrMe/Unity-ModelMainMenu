using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Utilitaire statique fournissant des méthodes de chiffrement et de déchiffrement basées sur l'algorithme AES.
/// Utilisé pour sécuriser les fichiers de sauvegarde locaux du jeu (empêche la triche et la corruption).
/// </summary>
public static class CryptoUtility
{
    #region Constants / Internal State
    /* 
     * ATTENTION : Pour un jeu en production, il est recommandé d'obfusquer ces clés 
     * ou de les générer dynamiquement pour éviter qu'elles ne soient extraites trop facilement.
     */

    /// <summary>
    /// Clé de chiffrement secrète (Doit impérativement faire 32 octets/caractères pour l'AES-256).
    /// </summary>
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("TaCleSecreteDe32CaracteresIci123");

    /// <summary>
    /// Vecteur d'initialisation (Doit impérativement faire 16 octets/caractères).
    /// </summary>
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("TonVecteurIci123");
    #endregion

    #region Public Methods
    /// <summary>
    /// Chiffre une chaîne de caractères en clair en utilisant l'algorithme AES.
    /// </summary>
    /// <param name="plainText">Le texte brut à chiffrer (généralement la structure de sauvegarde au format JSON).</param>
    /// <returns>Une chaîne encodée en Base64 représentant les données chiffrées, prête à être écrite dans un fichier.</returns>
    public static string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    /// <summary>
    /// Déchiffre une chaîne de caractères (précédemment chiffrée par cette classe) pour retrouver le texte brut original.
    /// </summary>
    /// <param name="cipherText">La chaîne chiffrée encodée en Base64 lue depuis le fichier de sauvegarde.</param>
    /// <returns>La chaîne de caractères brute originale (le JSON prêt à être désérialisé par Unity).</returns>
    public static string Decrypt(string cipherText)
    {
        byte[] buffer = Convert.FromBase64String(cipherText);
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream(buffer))
            {
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }
    #endregion
}