using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Gère intelligemment le focus de l'interface utilisateur en fonction du périphérique d'entrée actuel.
/// Cache la sélection lorsqu'on utilise la souris, et restaure automatiquement le focus (ou un focus par défaut) 
/// dès qu'on touche au clavier ou à la manette pour garantir une navigation fluide.
/// </summary>
public class SmartUIFocus : MonoBehaviour
{
    #region Internal State
    [Header("Configuration")]
    [Tooltip("S'il n'y a pas d'historique de sélection, quel bouton doit être sélectionné par défaut à la manette/clavier ?")]
    public GameObject defaultFallbackButton;

    /// <summary>
    /// Mémorise le dernier objet UI qui a été sélectionné par le joueur.
    /// </summary>
    private GameObject lastSelected;

    /// <summary>
    /// Définit les deux grands paradigmes de contrôle de l'interface.
    /// </summary>
    private enum ControlMode { Mouse, GamepadOrKeyboard }

    private ControlMode currentMode = ControlMode.Mouse;
    #endregion

    #region Unity Life Cycle
    private void Start()
    {
        // Au démarrage, si un objet est déjà sélectionné (par l'EventSystem), on le sauvegarde 
        // puis on vide la sélection pour éviter un conflit visuel si le joueur démarre à la souris.
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelected = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void Update()
    {
        DetectInputDevice();

        if (currentMode == ControlMode.Mouse)
        {
            // Mode Souris : On empêche l'EventSystem de garder un bouton "en surbrillance" de force.
            // On mémorise la sélection si le joueur a cliqué sur quelque chose, puis on l'efface.
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                lastSelected = EventSystem.current.currentSelectedGameObject;
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
        else
        {
            // Mode Manette/Clavier : Unity a impérativement besoin d'un bouton sélectionné pour naviguer.
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                // On essaie de redonner le focus au dernier bouton connu
                if (lastSelected != null && lastSelected.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(lastSelected);
                }
                // Sinon, on se rabat sur le bouton par défaut (ex: le bouton "Reprendre" en pause)
                else if (defaultFallbackButton != null && defaultFallbackButton.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(defaultFallbackButton);
                }
            }
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Vérifie à chaque frame quel périphérique envoie des inputs et met à jour le mode de contrôle en conséquence.
    /// </summary>
    private void DetectInputDevice()
    {
        // Détection Souris (Mouvement significatif ou Clic gauche)
        if (Mouse.current != null && (Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f || Mouse.current.leftButton.wasPressedThisFrame))
        {
            currentMode = ControlMode.Mouse;
        }

        // Détection Clavier (N'importe quelle touche pressée)
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            currentMode = ControlMode.GamepadOrKeyboard;
        }

        // Détection Manette (Bouton pressé, Joystick bougé ou Croix directionnelle touchée)
        if (Gamepad.current != null)
        {
            if (IsAnyGamepadButtonPressed() ||
                Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f ||
                Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.1f)
            {
                currentMode = ControlMode.GamepadOrKeyboard;
            }
        }
    }

    /// <summary>
    /// Parcourt tous les contrôles de la manette actuelle pour vérifier si un bouton classique a été pressé.
    /// </summary>
    /// <returns>Vrai si un bouton de la manette a été pressé durant cette frame, Faux sinon.</returns>
    private bool IsAnyGamepadButtonPressed()
    {
        if (Gamepad.current == null) return false;

        foreach (var control in Gamepad.current.allControls)
        {
            if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame)
            {
                return true;
            }
        }
        return false;
    }
    #endregion
}