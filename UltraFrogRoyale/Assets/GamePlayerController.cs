using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GamePlayerController : NetworkBehaviour
{
    public Transform tongue;
    public float speed;
    public Transform nameSprite;

    private Rigidbody2D rigidBody;
    private Rigidbody2D cameraRigidBody;

    private Vector2 motionVector;
    private float currentAngle;
    private Vector2 lastMotionVector;

    private GameManager gameManager;
    private UIController uiController;

    [SyncVar(hook = "OnIsDead")]
    private bool isDead = false;

    [SyncVar]
    private Color playerColor;

    private bool tongueReady = true;

    [SyncVar]
    private bool tongueDeployed = false;

    [SyncVar(hook = "OnSetSize")]
    private float size = 1.0f;

    [SyncVar]
    private string playerName;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (isServer)
        {
            playerColor = Random.ColorHSV(0.0f, 1.0f, 1.0f, 1.0f, 0.5f, 1.0f);
            gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
            gameManager.AddPlayer(this);
            playerName = gameManager.GetName();
            name = playerName;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isClient)
        {
            uiController = GameObject.FindGameObjectWithTag("Canvas").GetComponent<UIController>();
            GetComponent<SpriteRenderer>().color = playerColor;
            nameSprite.GetComponent<TextMesh>().text = playerName;
        }
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public float GetPlayerSize()
    {
        return size;
    }

    private void Awake()
    {
    }

    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        cameraRigidBody = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Rigidbody2D>();
        cameraRigidBody.MovePosition(rigidBody.position);
        tongue.gameObject.SetActive(false);

        // sync vars don't always seem available on StartClient for all objects, but attempt to set them here
        if (isClient)
        {
            GetComponent<SpriteRenderer>().color = playerColor;
            nameSprite.GetComponent<TextMesh>().text = playerName;
        }
    }

    // Update is called once per frame
    void Update()
    {
        tongue.gameObject.SetActive(tongueDeployed);

        if (isLocalPlayer)
        {
            // Horizontal and Vertical are the arrow buttons
            Vector2 inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            motionVector = inputVector.normalized * speed;
            if (inputVector != Vector2.zero)
            {
                currentAngle = Mathf.Atan2(inputVector.y, inputVector.x) * Mathf.Rad2Deg;
            }

            // Jump is the spacebar in Unity default
            float fireInputValue = Input.GetAxisRaw("Jump");
            if (fireInputValue != 0.0f)
            {
                CmdFireTongue();
            }
        }
    }

    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            rigidBody.MovePosition(rigidBody.position + motionVector * Time.fixedDeltaTime);
            rigidBody.MoveRotation(currentAngle);

            // Keep the camera moving with the player so they are always in the center
            cameraRigidBody.MovePosition(rigidBody.position + motionVector * Time.fixedDeltaTime);
        }
    }

    public void GotEaten()
    {
        if (!isServer) return;
        isDead = true;
        int playersLeft = gameManager.RemovePlayer(this);
        RpcGameOver(playersLeft);
        RpcLeaderboardUpdate(gameManager.GetLeaderboard());
    }

    private void OnSetSize(float newSize)
    {
        if(!isServer)
        {
            this.size = newSize;
        }
        
        // the size of the frog is actually the area, when you eat another
        // frog your frog grows by the area it ate
        float lengthOfSide = Mathf.Sqrt(newSize);
        this.transform.localScale = new Vector3(lengthOfSide, lengthOfSide, 1.0f);

        if(isLocalPlayer)
        {
            var camera = cameraRigidBody.gameObject.GetComponent<Camera>();
            camera.orthographicSize = camera.orthographicSize + 1;
        }
    }
    private void OnIsDead(bool _isDead)
    {
        if(!isServer)
        {
            isDead = _isDead;
        }
        if (isDead)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(isServer)
        {
            if(other.tag != "tongue")
            {
                GamePlayerController victim = other.GetComponent<GamePlayerController>();
                if(victim != null)
                {
                    size += victim.size;
                    victim.GotEaten();
                }
            }
        }
    }

    [Command]
    private void CmdFireTongue()
    {
        StartCoroutine(FireTongue());
    }

    [ClientRpc]
    void RpcGameOver(int playersLeft)
    {
        if(isLocalPlayer)
        {
            uiController.ShowStatusText();
            uiController.SetStatusText("Game Over, you got eaten.");
            uiController.ShowLeaderboard();
        }
        else
        {
            if(playersLeft == 1)
            {
                // we have a winner
                uiController.ShowStatusText();
                uiController.SetStatusText("Winner! You're the last frog standing.");
                uiController.ShowLeaderboard();
            }
        }
    }

    [ClientRpc]
    void RpcLeaderboardUpdate(string lbText)
    {
        uiController.SetLeaderboardText(lbText);
    }

    private const float TONGUE_OUT_TIME = 0.25f;
    private const float TONGUE_RESET_TIME = 0.5f;

    private IEnumerator FireTongue()
    {
        // fires tongue for a period of time, then blocks firing again
        // for a period of time
        if (tongueReady)
        {
            tongueReady = false;
            tongueDeployed = true;
            yield return new WaitForSeconds(TONGUE_OUT_TIME);
            tongueDeployed = false;
            yield return new WaitForSeconds(TONGUE_RESET_TIME);
            tongueReady = true;
        }
    }
}
