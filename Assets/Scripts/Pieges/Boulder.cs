using Unity.Netcode;
using UnityEngine;

/// <summary>
/// G�re la boule de roche
/// </summary>
public class Boulder : NetworkBehaviour
{
    public float speed = 5f;
    public float damage = 30f;
    public int direction = -1; // -1 = random, 0 = x+, 1 = x-, 2 = z+, 3 = z-

    private Vector3 moveDirection;
    private Animator animator;
    private Rigidbody rb;

    private bool isDespawning = false;
    //Le principe c'est que la boule prend une direction random parmi les 4 directions cardinales et se d�place dans cette direction (Sauf si on la change depuis un autre script)

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            GetComponent<Collider>().enabled = false;
            Destroy(this);
        }
    }

    private void RandomizeDirection()
    {
        direction = Random.Range(0, 4);
    }

    private void Launch()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        if (direction == -1)
            RandomizeDirection();

        //En gros des que la boule touche le sol elle se d�place dans la direction donn�e
        switch (direction)
        {
            case 0:
                moveDirection = Vector3.right;
                animator.Play("RollZ+");
                break;
            case 1:
                moveDirection = Vector3.left;
                animator.Play("RollZ-");
                break;
            case 2:
                moveDirection = Vector3.forward;
                animator.Play("RollX+");
                break;
            case 3:
                moveDirection = Vector3.back;
                animator.Play("RollX-");
                break;
        }

        rb.velocity = moveDirection * speed;
    }

    /// <summary>
    /// Quand on rentre en collision avec un joueur, on lui inflige des d�g�ts
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<MonPlayerController>().Damage(damage);
        }
        else if (collision.gameObject.CompareTag("Cow"))
        {
            collision.gameObject.GetComponent<CowController>().UnCow();
            Despawn();
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            Despawn();
        }
    }

    /// <summary>
    /// Despawn la boule
    /// </summary>
    private void Despawn()
    {
        if (!isDespawning)
        {
            isDespawning = true;
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    private void Update()
    {
        CheckGround();
    }

    /// <summary>
    /// V�rifie si la boule touche le sol
    /// </summary>
    private void CheckGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f))
        {
            if (hit.collider.CompareTag("Floor"))
            {
                Launch();
            }
        }
    }
}
