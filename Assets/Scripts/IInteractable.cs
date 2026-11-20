using UnityEngine;

namespace LastLight
{
    /// <summary>
    /// Shared interface for cupboards, drawers, treasure chests, and locked exit doors
    /// supporting the contextual mobile "OPEN" button and tap-to-open interaction.
    /// </summary>
    public interface IInteractable
    {
        bool CanInteract { get; }
        string InteractionPrompt { get; }
        Vector3 InteractionWorldPosition { get; }
        void Interact(PlayerController player);
    }
}
