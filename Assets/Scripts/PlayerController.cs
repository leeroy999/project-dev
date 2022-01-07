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
    [SerializeField] private float _jumpForce = 100f;
    [Range(0, .3f)] [SerializeField] private float _movementSmoothing = .1f;
    [SerializeField] private LayerMask _colliders;
    [SerializeField] private Transform _groundChecker;
    [SerializeField] private TextMesh _playerName;

    private const float _groundedRadius = .075f;
    private bool _isGrounded;
    private Rigidbody2D _body;
    private Vector2 _velocity = Vector2.zero;
    private float _horizontalMove = 0f;
    private bool _isJump = false;

    private PhotonView _view;
    private SpriteRenderer _sprite;


    // Start is called before the first frame update
    private void Start()
    {
        _body = GetComponent<Rigidbody2D>();
        _view = GetComponent<PhotonView>();
        _sprite = GetComponent<SpriteRenderer>();
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
            this.photonView.RPC("ChangeName", RpcTarget.All, PhotonNetwork.NickName);
            _horizontalMove = Input.GetAxisRaw("Horizontal") * _speed;
            if (Input.GetAxisRaw("Vertical") > 0)
            {
                _isJump = true;
            }

            if (transform.position.y < -8)
            {
                transform.position = GameManager.SpawnPoint;
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
        // Move our character
		Move(_horizontalMove * Time.fixedDeltaTime);
        _isJump = false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_sprite.flipX);
		}
		else
		{
			_sprite.flipX = (bool) stream.ReceiveNext();
		}
	}

    private void Move(float move) 
    {
        // Move the character by finding the target velocity
        Vector3 targetVelocity = new Vector2(move * 10f, _body.velocity.y);
        // And then smoothing it out and applying it to the character
        _body.velocity = Vector2.SmoothDamp(_body.velocity, targetVelocity, ref _velocity, _movementSmoothing);

        if (move < 0)
        {
            // flip the player.
            _sprite.flipX = true;
        } else if (move > 0)
        {
            _sprite.flipX = false;
        }

        if (_isJump && _isGrounded) 
        {
            _isGrounded = false;
            _body.AddForce(new Vector2(0f, _jumpForce));
        }
    }

    [PunRPC]
    private void ChangeName(string name)
    {
        _playerName.text = name;
    }
}
