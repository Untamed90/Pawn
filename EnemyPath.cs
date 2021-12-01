using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    public float speed;
    Vector3 direction;
    private Transform player;
    public float lineOfSight;
    public bool movingRight;

    public bool WalkActivated = true;
    public bool followsPlayer; //true if enemy can follow the player
    public bool teleports; //true if enemy can teleport
    public float teleportTimer = 5f;
    public float teleportTimerCounter;

    public bool blockedByWall;

    public GameObject room; //room where the enemy is located

    public AudioSource audioSource;
    public AudioClip teleportSound;

    Enemy enemyScript;
    public bool knight; //true if the enemy is a knight
    public bool playerInRoom; //true if player is in the same room

    // Start is called before the first frame update
    void Start()
    {
        //stop audio and particles at the start, attach gameobjects
        knight = this.GetComponentInParent<Enemy>().knight;
        audioSource.Stop();
        enemyScript = this.GetComponentInParent<Enemy>();
        if(this.GetComponent<ParticleSystem>())this.GetComponent<ParticleSystem>().Stop();

        if (!followsPlayer)
        {
            direction = new Vector3(-1, 0, 0);
            if (teleports) { 
                teleports = false;
                Debug.LogError("An enemy cannot teleport if it does not follow the player");
            }
        }
        else
        {
            //initialize timer counter for teleportation
            if (teleports) teleportTimerCounter = teleportTimer;
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    void Update()
    {
        //check if player is in the same room
        playerInRoom = enemyScript.CheckIfPlayerIsThere();

        //flip enemy if necessary
        movingRight = !this.GetComponent<SpriteRenderer>().flipX;

        //if the enemy can walk, activate the right walking mode
        if (WalkActivated)
        {
            if (!followsPlayer)
                straightWalk();
            else if (teleports)
                teleport();
            else
                followPlayer();

            float distance = 20f;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.TransformDirection(Vector2.up), distance, 2);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector2.left)*distance, Color.red);

            if (hit && (hit.collider.gameObject.tag == "Wall" || hit.collider.gameObject.tag == "Door"))
                { blockedByWall = true;}
            else
                { blockedByWall = false; }
        }

        //if the enemy can teleport, activate it when counter is at 0
        //they can only teleport then
        if (teleports)
        {
            teleportTimerCounter -= Time.deltaTime;

            if(teleportTimerCounter <= 0)
            {
                this.GetComponentInParent<EnemyPath>().WalkActivated = true;
            }
        }

    }

    void straightWalk()
    {
        if (room)
        {
                float roomradius = room.GetComponent<BoxCollider2D>().size.x / 2;
                float roomLeftLimit = room.GetComponent<Transform>().position.x - roomradius;
                float roomRightLimit = room.GetComponent<Transform>().position.x + roomradius;
        
            //switch direction when the enemy has reached the end of the room it's in
            if (this.GetComponentInParent<Transform>().position.x <= roomLeftLimit ||
                this.GetComponentInParent<Transform>().position.x >= roomRightLimit - 0.5f) {
                direction.x *= -1; 
            }
    }

        //starts walking in a direction
        transform.Translate(direction * speed * Time.deltaTime);
        
    }

    //flip the sprite when the enemy hits something
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((!followsPlayer) && (collision.gameObject.tag == "Door") || collision.gameObject.tag == "Wall")) {
            direction.x *= -1;
            this.GetComponent<SpriteRenderer>().flipX = !this.GetComponent<SpriteRenderer>().flipX;
        }
    }

    //enemy follows player if it's in its line of sight
    void followPlayer() {
        float distanceFromPlayer = Vector2.Distance(player.position, transform.position);
        
        if (distanceFromPlayer < lineOfSight && enemyScript.CheckIfPlayerIsThere()) { 
            transform.position = Vector2.MoveTowards(this.transform.position, player.position, speed*Time.deltaTime);
        }

        enemyScript.Flip();
    }

    //enemy teleports on the other side of the player if they are in the same room and close enough
    void teleport()
    {
        if (playerInRoom)
        {
            teleportTimerCounter = teleportTimer;
            
            this.GetComponentInParent<EnemyPath>().WalkActivated = false;

            if (!enemyScript.CheckIfPlayerIsClose())
            {
                //calculate new position based on player's position (left or right to the enemy)
                Vector3 newPos;
                if (enemyScript.IsPlayerOnTheRight())
                {
                   newPos = new Vector3(player.position.x + 1f, player.position.y, player.position.z);
                   if(knight) this.GetComponent<BoxCollider2D>().offset = new Vector2(this.GetComponent<BoxCollider2D>().offset.x + 0.5f, this.GetComponent<BoxCollider2D>().offset.y);
                }
                else
                {
                   newPos = new Vector3(player.position.x - 1f, player.position.y, player.position.z);
                    if (knight) this.GetComponent<BoxCollider2D>().offset = new Vector2(this.GetComponent<BoxCollider2D>().offset.x - 0.5f, this.GetComponent<BoxCollider2D>().offset.y);
                }

                //play sounds and particles
                if(this.GetComponent<ParticleSystem>())this.GetComponent<ParticleSystem>().Play();
                audioSource.clip = teleportSound;
                audioSource.Play();

                //change position
                this.GetComponent<Transform>().position = newPos;
           }
            
                teleportTimerCounter = teleportTimer;
        }
    }
}
