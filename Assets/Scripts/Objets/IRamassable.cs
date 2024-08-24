/// <summary>
/// Interface des p qui r�presente un objet avec un poids (Qui peut �tre tenu par le joueur)
/// </summary>
public interface IRamassable
{
    /// <summary> 
    /// Change l'etat de l'objet si il est tenu ou non
    /// </summary>
    /// <param name="newState">Le nouvel etat de l'objet</param>
    public void ChangeState(bool newState);
}