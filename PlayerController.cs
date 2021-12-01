
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour {

	GameObject player;

	//variable for Move
	[Header("Move")]
	public float movespeed = 1;
	public bool movingRight;
	public ParticleSystem dust;
	public bool canMove = true;
	Vector2 moveV;

	//variables for Jump
	[Header("Jump")]
	public float jumpForce = 1;
	public bool isGrounded;
	private float jumpTimeCounter;
	public float jumpTime; //for how long can we hold to jump higher
	public bool isJumping;

	//variables for attacks
	[Header("Attack")]
	public int activatedWeapon;
	GameObject enemyTargeted;
	float currentY;
	public int currentAttackPower;
	public GameObject arrow;
	Text arrowNb;
	public float hurtForce = 200f;

	//variables for shield
	[Header("Protect")]
	public bool shieldActivated;


	//variables for animation
	[Header("Others")]
	public Animator animator;
	public GameObject currentRoom;

	[Header("Audio")]
	public AudioSource audioSource;
	public AudioClip hurtSound;
	public AudioClip weaponSound;
	public AudioClip collectibleSound;

	[Header("Inputs")]
	public string attackInput;
	public string protectInput;
	PlayerControls controls;

	private void Awake()
	{
		player = GameObject.Find("Player");
		arrowNb = GameObject.Find("ArrowsNb").GetComponent<Text>();
		audioSource = this.gameObject.GetComponent<AudioSource>();

		//Controls
		controls = new PlayerControls();
		controls.GamePlay.Attack.performed += ctx => Attack();
		controls.GamePlay.Jump.performed += ctx => Jump();
		controls.GamePlay.Shield.performed += ctx => Protect();
		controls.GamePlay.Shield.canceled += ctx => StopProtecting();

		//Move controls
		controls.GamePlay.Move.performed += ctx => moveV = ctx.ReadValue<Vector2>();
		controls.GamePlay.Move.canceled += ctx => moveV = Vector2.zero;
		
	}

	void Start() {
		activatedWeapon = 1;
		audioSource.Stop();
		audioSource.clip = hurtSound;
		currentY = this.gameObject.GetComponent<BoxCollider2D>().size.y;
		movingRight = true;
		animator = GameObject.Find("Player Renderer").GetComponent<Animator>();
		dust = GameObject.Find("DustParticles").GetComponent<ParticleSystem>();
		GameObject.Find("TeleportParticles").GetComponent<ParticleSystem>().Stop();

	}

    private void FixedUpdate()
    {
		if (animator.GetBool("Dead") == false && canMove)
		{
			SetAttackPower(activatedWeapon);
			Move();
		}
	}

	//Enable new input system
    void OnEnable()
	{
		controls.GamePlay.Enable();

	}

	//Disable new input System
    void OnDisable()
    {
		controls.GamePlay.Disable();
    }

	//Move player left or right based on control inputs
    void Move() {
		//Constantly check if we're moving the player
		Vector3 movement = new Vector3(moveV.x, 0f, 0f);
		//the position of the transform(the player) changes with the movement, the speed and time
		transform.position += movement.normalized * Time.deltaTime * movespeed;


		//Play dust particles when player moves
		if (moveV.x != 0f)
		{
			dust.Play();
			animator.SetBool("IsWalking", true);
		}

		if (moveV.x == 0)
		{
			dust.Stop();
			animator.SetBool("IsWalking", false);

		}

		//CHaracter looks left or right depending on its direction
		Flip();
	}

	void Jump() {
		//First regular jump. can only start jumping when on the ground and when we hit the button
		//Timer gets set, jumping state is activated
		if (isGrounded) {

			animator.SetTrigger("Jump");
			isJumping = true;
			jumpTimeCounter = jumpTime;
			gameObject.GetComponent<Rigidbody2D>().AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
		}

		//while the jumping state is activated and we HOLD the button
		//the counter counts down to 0 and adds force during that time
		//when the counter is at 0 (max time that we can hold the button),
		//the jumping state becomes false so that we can no longer add force while holding the button
		if (isJumping == true) {
			if (jumpTimeCounter > 0) {
				gameObject.GetComponent<Rigidbody2D>().AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
				jumpTimeCounter -= Time.deltaTime;
			} else {
				isJumping = false;
			}
		}

		//when we release the button, jumping state becomes false so that we can't add force while its in the air
		isJumping = false;
	}


	//CHaracter looks left or right depending on its direction
	void Flip() {

		//Euler angles can represent a three dimensional rotation by performing three separate rotations around individual axes
		if (moveV.x > 0) {
			transform.eulerAngles = new Vector3(0, 0, 0);
			movingRight = true;
		}

		if (moveV.x < 0) {
			transform.eulerAngles = new Vector3(0, 183, 0);
			movingRight = false;
		}
	}

	//Attack power changes depending on the activated weapon.
	//1 = dagger, 2 = bow, 4 = sword
	void SetAttackPower(int activatedWeapon)
	{
		if (activatedWeapon == 1)
		{
			currentAttackPower = 1;
		}
		else if (activatedWeapon == 2) {
			currentAttackPower = 2;
		}
		else if (activatedWeapon == 4)
		{
			currentAttackPower = 4;

		}
	}

	//Player attacks with right weapon, only if they're not using the shield
	void Attack() {

		if (shieldActivated == false)
		{
			if (activatedWeapon == 1)
			{
				//dagger
				animator.SetTrigger("AttackWithDagger");
				HurtSomeone();
			}
			else if (activatedWeapon == 2 && player.GetComponent<PlayerStats>().nbArrows > 0)
			{
				//bow
				animator.SetTrigger("AttackWithBow");
				Vector3 playerPos = this.gameObject.transform.position;
				Quaternion arrowQuaternion = Quaternion.Euler(0, 180, 180);
				Vector3 arrowPosition = new Vector3(playerPos.x + 0.2f, playerPos.y, playerPos.z);
				Instantiate(arrow, arrowPosition, arrowQuaternion);
				player.GetComponent<PlayerStats>().nbArrows--;

				//update ui
				arrowNb.text = GameObject.Find("Player").GetComponent<PlayerStats>().nbArrows.ToString();
			}
			else if (activatedWeapon == 4)
			{
				//sword
				animator.SetTrigger("AttackWithSword");
				HurtSomeone();
			}
		}
	}

	//Gives damage to enemy depending on if it's the boss or not
	public void HurtSomeone()
    {
		if (enemyTargeted && enemyTargeted.name != "Queen")
		{
			enemyTargeted.GetComponent<Enemy>().life -= currentAttackPower;
			enemyTargeted.GetComponent<Animator>().SetTrigger("Hurt");
			audioSource.clip = hurtSound;
			audioSource.Play();
			enemyTargeted.GetComponent<Enemy>().GetPushedAway();
		}
		else if (enemyTargeted && enemyTargeted.name == "Queen")
		{
			if (!enemyTargeted.GetComponent<Queen>().shieldActivated == true)
			{
				enemyTargeted.GetComponent<Queen>().life -= currentAttackPower;
				enemyTargeted.GetComponent<Animator>().SetTrigger("Hurt");
				audioSource.clip = hurtSound;
				audioSource.Play();
				enemyTargeted.GetComponent<Queen>().GetPushedAway();
			}
		}

	}

	//Use shield
	void Protect()
	{
		if (GameObject.Find("Shield").GetComponent<Weapon>().owned == true)
		{
			GameObject.Find("Shield").GetComponent<SpriteRenderer>().enabled = true;
			shieldActivated = true;
		}
	}

	//Stop using shield
	public void StopProtecting()
	{
		if (GameObject.Find("Shield").GetComponent<Weapon>().owned == true)
		{
			GameObject.Find("Shield").GetComponent<SpriteRenderer>().enabled = false;
			shieldActivated = false;
		}
	}

	//Targets an enemy to attack
	private void OnCollisionEnter2D(Collision2D collision)
    {
		if (collision.gameObject.tag == "Enemy")
		{
			enemyTargeted = collision.gameObject;
		}
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.gameObject == enemyTargeted)
			enemyTargeted = null;
		isGrounded = false;
	}

	private void OnCollisionStay2D(Collision2D collision)
	{

		if (collision.gameObject.tag == "Ground")
		{
			isGrounded = true;
		}
	}

	//Player sound when player finds something
	public void PlayCollectibleSound()
    {
		audioSource.clip = collectibleSound;
		audioSource.Play();
    }
}
