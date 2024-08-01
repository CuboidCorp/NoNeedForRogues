using Unity.Netcode;

/// <summary>
/// Interface des p qui répresente un objet avec un poids (Qui peut être tenu par le joueur)
/// </summary>
public interface Ramassable
{
    public float weight = 1;

    /// <summary>
    /// Change l'etat de l'objet si il est tenu ou non
    /// </summary>
    /// <param name="newState">Le nouvel etat de l'objet</param>
    public void ChangeState(bool newState);
}
