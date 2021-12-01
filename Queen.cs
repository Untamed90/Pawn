
using UnityEngine;
using System.Collections;

public class Queen : MonoBehaviour
{
    [Header("Attack")]
    bool queenAttacks = false;
    public int damage;
    public float hurtForce;
    ForceMode2D forceMode = ForceMode2D.Force;
    Coroutine currentState;

    [Header("Life & Shield")]
    public float life = 10;
    public float timeBeforeDestroy = 3f;
    public bool shieldActivated = false;
    public GameObject deathParticles;

    [Header("Teleportation")]
    Vector3 distanceVector;
    public float teleportDistance = 1f;
    public ParticleSystem teleportParticles;

    [Header("Player References")]
    GameObject player;
    Transform playerTransform;
    PlayerStats playerStats;
    float playerForce;

    [Header("Queen Enemy references")]
    Animator anim;
    public GameObject room;
    public GameObject cell;
    public AudioSource audioSource;
    public AudioClip hurtS;

    void Start()
    {
        //deactivate death particles when not in used
        deathParticles.GetComponent<ParticleSystem>().Stop();
        deathParticles.SetActive(false);

        //assign gameobjects
        anim = this.gameObject.GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        playerTransform = player.GetComponent<Transform>();
        playerStats = player.GetComponent<PlayerStats>();
        playerForce = player.GetComponent<PlayerController>().hurtForce;

        ChangeState(LoopTwo());

    }

    void Update()
    {
        //===== DON'T REMOVE
        DistanceFromPlayer();
        Flip();

        //Die
        if (life <= 0f)
        {
            StartCoroutine(Die());
        }
    }


    //Change
    void ChangeState(IEnumerator nextState)
    {
        if (currentState != null)
            StopCoroutine(currentState);

        if(life > 0)currentState = StartCoroutine(nextState);
        
    }

    //=========================================
    //LOOPS
    //Each loop is triggered at a certain number of lives
    //LoopTwo is the first one
    //LoopFour happens when Queen has only 4 to 7 lives
    //LoopOne is triggered when Queen has less than 4 lives


    IEnumerator LoopOne()
    {
        do
        {
            for (int i = 5; i > 0; i--)
            {

                Teleport();
                yield return new WaitForSeconds(0.5f);
                Attack();
                yield return new WaitForSeconds(0.75f);
                queenAttacks = false;

            }

            yield return new WaitForSeconds(5);
            Protect();
            yield return new WaitForSeconds(5);
            StopProtecting();
            yield return new WaitForSeconds(5);
        } while (life <= 3);

    }

    IEnumerator LoopTwo()
    {
        do
        {
            for (int i = 2; i > 0; i--)
            {
                Teleport();
                yield return new WaitForSeconds(0.75f);
            }

            Attack();
            yield return new WaitForSeconds(1.25f);
            queenAttacks = false;

            Teleport();

            //idle
            yield return new WaitForSeconds(5);

            Protect();
            yield return new WaitForSeconds(4);
            StopProtecting();
            yield return new WaitForSeconds(1);
        } while (life > 7);

        ChangeState(LoopFour());
    }

    IEnumerator LoopFour()
    {
        do
        {
            for (int i = 2; i > 0; i--)
            {
                yield return new WaitForSeconds(1f);
                Attack();
                yield return new WaitForSeconds(1.25f);
                queenAttacks = false;
            }

            Protect();
            yield return new WaitForSeconds(4);
            StopProtecting();
            yield return new WaitForSeconds(0.75f);

            Teleport();
            //idle
            yield return new WaitForSeconds(4);
        }while (life > 3 && life <= 7) ;

        ChangeState(LoopOne());
    }


    //=========================================
    //INDIVIDUAL ABILITIES AND ACTIONS

    //Shield
    public void Protect()
    {
        anim.SetBool("IsProtecting", true);
       shieldActivated = true;
    }

    public void StopProtecting()
    {

        anim.SetBool("IsProtecting", false);
        shieldActivated = false;
    }

    //Hurt
    public void GetPushedAway()
    {
        this.GetComponentInParent<Rigidbody2D>().AddForce(Vector2.up * playerForce, forceMode);
        if (IsPlayerOnTheRight() == false)
            this.GetComponentInParent<Rigidbody2D>().AddForce(Vector2.right * playerForce, forceMode);
        else
            this.GetComponentInParent<Rigidbody2D>().AddForce(Vector2.left * playerForce, forceMode);
    }

    IEnumerator Die()
    {
        //deactivate player controls and queen movements
        player.GetComponent<PlayerController>().enabled = false;
        ChangeState(null); 
        anim.SetBool("Dead", true);
        this.GetComponent<BoxCollider2D>().enabled = false;
        this.GetComponent<Rigidbody2D>().simulated = false;

        yield return new WaitForSeconds(1.5f);
        deathParticles.SetActive(true);
        this.GetComponentInParent<SpriteRenderer>().enabled = false;

        //open king's cell
        cell.GetComponentInChildren<Cage>().queenIsDead = true;
        Destroy(this.gameObject, timeBeforeDestroy);

    }

    //Triggers animation and stops walking when attacking
    void Attack()
    {
        if (life > 0)
        {
            this.GetComponentInParent<EnemyPath>().WalkActivated = false;
            anim.SetTrigger("IsAttacking");

            queenAttacks = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //if queen touches player WHEN ATTACKING
        if (queenAttacks)
        {
            queenAttacks = false;
            playerStats.lives -= damage;
            GameObject.Find("Player Renderer").GetComponent<Animator>().SetTrigger("Hurt");

            //throws player away
            ForceMode2D forceMode = ForceMode2D.Force;
            GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>().AddForce(Vector2.up * hurtForce, forceMode);
            if (IsPlayerOnTheRight() == false)
                GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>().AddForce(Vector2.left * hurtForce, forceMode);
            else
                GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>().AddForce(Vector2.right * hurtForce, forceMode);
        }
    }

    void Teleport()
    {
        Vector3 newPos;

        //change newPos if outside room boundary
        float roomradius = room.GetComponent<BoxCollider2D>().size.x / 2;
        float roomLeftLimit = room.GetComponent<Transform>().position.x - roomradius;
        float roomRightLimit = room.GetComponent<Transform>().position.x + roomradius;

        //teleports based on player's position
        if (IsPlayerOnTheRight())
        {
            newPos = new Vector3(playerTransform.position.x + teleportDistance, this.transform.position.y, this.transform.position.z);
            //adjust queen's position if out of bounds
            if(newPos.x >= roomRightLimit)
            {
                newPos.x = roomRightLimit - 1.3f;
            }
        }
        else
        {
            newPos = new Vector3(playerTransform.position.x - (teleportDistance + 1f), this.transform.position.y, this.transform.position.z);
            //adjust queen's position if out of bounds
            if (newPos.x <= roomLeftLimit)
            {
                newPos.x = roomLeftLimit + 1.5f;
            }

        }


        if (teleportParticles) teleportParticles.Play();
        this.GetComponent<Transform>().position = newPos;
    }

    //Calculations in relation to player
    public void DistanceFromPlayer()
    {
        Vector3 playerPos = playerTransform.position;
        Vector3 currentPos = this.gameObject.GetComponent<Transform>().position;
        distanceVector = playerPos - currentPos;
    }

    //check if player is on the right or left
    public bool IsPlayerOnTheRight()
    {

        if (distanceVector.x > 0)
            return true;

        return false;
    }

    //Switch sprite orientation
    public void Flip()
    {
        if (IsPlayerOnTheRight())
        {
            this.GetComponentInParent<SpriteRenderer>().flipX = false;
            this.GetComponentInParent<BoxCollider2D>().offset = new Vector2(-0.8f, 1.96f);
        }
        else
        {
            this.GetComponentInParent<SpriteRenderer>().flipX = true;
            this.GetComponentInParent<BoxCollider2D>().offset= new Vector2(0.97f, 1.96f);
        }
    }
}
