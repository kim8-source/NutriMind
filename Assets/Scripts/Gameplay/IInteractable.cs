namespace NutriMind.Gameplay
{
    // Anything the player can "Talk" or "Interact" with.
    public interface IInteractable
    {
        string PromptText { get; }   // label on the Interact button, for example "Talk"
        bool CanInteract { get; }
        void Interact();
    }
}
