using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviourPun, IPunObservable
{
    public float _speed = 20f;
    public int Health = 100;
    [SerializeField] private float _jumpForce = 4;
    [Range(0, .3f)] [SerializeField] private float _movementSmoothing = .1f;
    [SerializeField] private LayerMask _colliders;
    [SerializeField] private Transform _groundChecker;
    [SerializeField] private TextMesh _playerName;
    [SerializeField] private TextMesh _health;

    private const float _groundedRadius = .075f;
    private bool _isGrounded;
    private Rigidbody2D _body;
    private Animator _anim;
    private Vector2 _velocity = Vector2.zero;
    private float _horizontalMove = 0f;
    private bool _isJump = false;

    private PhotonView _view;
    private SpriteRenderer _sprite;
    private bool _isHurting = false;
    private Vector3 correctPlayerPos = Vector3.zero;


    // Start is called before the first frame update
    private void Start()
    {
        _body = GetComponent<Rigidbody2D>();
        _view = GetComponent<PhotonView>();
        _sprite = GetComponent<SpriteRenderer>();
        _anim = GetComponent<Animator>();
        if (!_view.ObservedComponents.Contains(this))
		{
			_view.ObservedComponents.Add(this);
		}
    }

    // Update is called once per frame
	private void Update () 
    {
        if (_view.IsMine)
        {
            this.photonView.RPC("UpdateHealth", RpcTarget.All);
            this.photonView.RPC("ChangeName", RpcTarget.All, PhotonNetwork.NickName);
            _horizontalMove = !_isHurting 
                ? Input.GetAxisRaw("Horizontal") * _speed * Mathf.Min(Mathf.Abs(Health), 100) / 100
                : 0;
            _anim.SetFloat("speed", Mathf.Abs(_horizontalMove));
            if (Input.GetAxisRaw("Vertical") > 0 && !_isHurting)
            {
                _isJump = true;
            }

            if (Input.GetKey("r"))
            {
                this.photonView.RPC("Respawn", RpcTarget.All);
            }

            if (transform.position.y < -8)
            {
                this.photonView.RPC("Respawn", RpcTarget.All);
            }
        } else {
            float distance = Vector3.Distance(transform.position, this.correctPlayerPos);
            if (distance < 2f)
            {
                transform.position = Vector3.Lerp(transform.position, this.correctPlayerPos, Time.deltaTime * 5);
            }
            else
            {
                transform.position = this.correctPlayerPos;
            }
        }
    }

    // FixedUpdate is called every physics update
    private void FixedUpdate()
    {
        _isGrounded = false;
		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
		Collider2D[] colliders = Physics2D.OverlapCircleAll(_groundChecker.position, _groundedRadius, _colliders);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
				_isGrounded = true;
               
		}
        if (_isGrounded) {
            _anim.SetBool("isJumping", false);
        } else if (_isJump) {
            _anim.SetBool("isJumping", true);
        }
        // Move our character
        Move(_horizontalMove * Time.fixedDeltaTime);
        _isJump = false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_sprite.flipX);
            stream.SendNext(transform.position);
		}
		else
		{
			_sprite.flipX = (bool) stream.ReceiveNext();
            this.correctPlayerPos = (Vector3)stream.ReceiveNext();
		}
	}

    private void Move(float move) 
    {
        // Move the character by finding the target velocity
        Vector2 targetVelocity = new Vector2(move * 10f, _body.velocity.y);
        // And then smoothing it out and applying it to the character
        _body.velocity = Vector2.SmoothDamp(_body.velocity, targetVelocity, ref _velocity, _movementSmoothing);

        if (move < 0)
        {
            // flip the player.
            _sprite.flipX = true;
            
        } else if (move > 0)
        {
            _sprite.flipX = false;
        } else {
        }

        if (_isJump && _isGrounded) 
        {
            _isGrounded = false;
            _body.velocity =  new Vector2(_body.velocity.x, _jumpForce);
            _isJump = false;
        }
    }

    [PunRPC]
    private void ChangeName(string name)
    {
        _playerName.text = name;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("touch");
        if (collision.gameObject.layer == 9) // hurting layer
        {
            Debug.Log("ouch!!");
            StartCoroutine(Hurting());
        }
    }

    [PunRPC]
    public void Injured()
    {
        if (_view.IsMine)
        {
            Health -= 10;
        }
    }

    private IEnumerator Hurting()
    {
        _isHurting = true;
        this.photonView.RPC("Injured", RpcTarget.All);
        bool alpha = true;
        for (int i = 0; i <= 10; i++)
        {
            _sprite.color = new Color(_sprite.color.r, _sprite.color.g, _sprite.color.b,
                alpha ? 255 : 0);
            alpha = !alpha;
            yield return new WaitForSeconds(0.1f);
        }
        _isHurting = false;
    }

    [PunRPC]
    private void Respawn()
    {
        if (_view.IsMine)
        {
            transform.position = GameManager.SpawnPoint;
            Health = 100;
        }
    }

    [PunRPC]
    private void UpdateHealth()
    {
        if (_view.IsMine)
        {
            _health.text = Health.ToString() + " Health";
        }
    }
}
