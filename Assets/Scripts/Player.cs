using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class Player : NetworkBehaviour
{
    //For when players move onto different surfaces to adjust movement
    //Define tuple struct for surface movement modifier values
    private struct SurfaceModifiers
    {
        public readonly float MovementMultiplier;
        public readonly float TorqueMultiplier;
        public readonly float Drag;

        public SurfaceModifiers(float movement, float torque, float drag)
        {
            MovementMultiplier = movement;
            TorqueMultiplier = torque;
            Drag = drag;
        }
    }
    // Initialize surface modifiers dictionary
    private static readonly Dictionary<string, SurfaceModifiers> surfaceModifiers = new()
    {
        { "Ground", new SurfaceModifiers(1f, 1f, 0.1f) },
        { "Ice", new SurfaceModifiers(1f, 3f, 0f) },
        { "Sand", new SurfaceModifiers(0.8f, 0.7f, 2f) }
    };

    //Variables
    [SerializeField] private Transform camTarget;   //Target of the camera
    [SerializeField] private Renderer _renderer;
    public float baseSpeed = 7500f;                 //Player speed
    public float baseTorqueAmount = 1000f;          //Player torque
    private string _currentSurfaceTag = "Ground";   //Current surface
    public AudioClip eliminationSound;              //Elimination sound
    public AudioClip collisionSound;                //Audio for collision sounds
    private AudioSource _audioSource;
    public float soundCooldown = 0.1f;              //Variables so collision sound doesn't infinite
    public float lastSoundTime = 0;
    private NetworkRigidbody3D _networkRigidbody3D; //network rigidbody component
    private Rigidbody _rigid;                       //Player rigidbody component
    public Rigidbody Rigidbody => _rigid;           // Expose the rigidbody for external access
    public NetworkRigidbody3D NetworkRigidbody3D => _networkRigidbody3D;

    //Objects
    public float enableMovementTime { get; private set; } = 0;             //Movement delay at game start
    [Networked] public string Name { get; private set; } = "default";      //Network object to store player name
    [Networked] public bool IsEliminated { get; set; } = false;            //Store player elimination status
    [Networked] public PlayerRef LastCollidedPlayer { get; set; }          //Store the last collided player
    [Networked] public int Score { get; set; }                             //Store the players score
    [Networked] public bool IsReady { get; set; }                          //Is the player ready
    [Networked] public int ColourIndex { get; set; }                       //Players colour
    
    //A list of colours to be used by players
    private static readonly List<Color> colours = new()
    {
        new Color(0.1f, 0.7f, 0.3f),  //Green
        new Color(0.7f, 0.2f, 0.3f),  //Red
        new Color(0.2f, 0.3f, 0.7f),  //Blue
        new Color(0.9f, 0.9f, 0.2f),  //Yellow
        new Color(0.6f, 0.3f, 0.7f),  //Purple
        new Color(0.1f, 0.6f, 0.9f),  //Cyan
        new Color(0.9f, 0.4f, 0.1f),  //Orange
        new Color(0.8f, 0.1f, 0.5f),  //Pink
        new Color(0.0f, 0.5f, 0.5f)   //Teal
    };

    public void Awake()
    {
        //initialise the components of player
        _rigid = GetComponent<Rigidbody>();
        _networkRigidbody3D = GetComponent<NetworkRigidbody3D>();
        _audioSource = GetComponent<AudioSource>();
    }

    //Handling collisions between the player and other objects
    private void OnCollisionEnter(Collision collision) {
        //If the collision is with another player
        if (collision.gameObject.CompareTag("Player")) {
            // Delegate collision handling to the GameLogic
            if (GameLogic.Singleton != null) {
                GameLogic.Singleton.HandlePlayerCollision(this, collision.gameObject.GetComponent<Player>());
            } else {
                Debug.LogError("GameLogic instance not found!");
            }
        }

        if (surfaceModifiers.ContainsKey(collision.collider.tag)) {
            _currentSurfaceTag = collision.collider.tag;
        } else {
            _currentSurfaceTag = "Ground"; // Default to "Ground"
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        
        if (Runner.IsServer) {
            //Server assigns a random color index
            ColourIndex = Random.Range(0, colours.Count);
        }

        // Apply the synchronized color index to material
        _renderer.material.color = colours[ColourIndex];

        
        if (HasInputAuthority)
        {
            Runner.GetComponent<InputManager>().localPlayer = this;
            //Store player name and send to other clients via RPC
            Name = PlayerPrefs.GetString("Photon.Menu.Username");
            RPC_PlayerName(Name);
            //Set cam target to player
            CameraFollow.Singleton.SetTarget(camTarget);
        }
    }

    public override void FixedUpdateNetwork()
    {
        //Skip movement updates if the game is counting down
        if (Time.time < enableMovementTime) return;
        
        //When receiving inputdata from NetInput class
        if (GetInput(out NetInput inputData))
        {
            //Create a vector from the horizontal+vertical movement, and normlise the values
            var inputVector = new Vector3(inputData.horizontal, 0, inputData.vertical).normalized;
            //Retrieve the camera rotation
            var cameraRotation = inputData.cameraRotation;
            //Transform input vector to align with the camera's direction, so W is relative to camera
            var worldInputVector = cameraRotation * inputVector;

            // Apply surface-specific multipliers
            var currentSpeed = baseSpeed;
            var currentTorque = baseTorqueAmount;
            if (surfaceModifiers.TryGetValue(_currentSurfaceTag, out var modifiers))
            {
                currentSpeed *= modifiers.MovementMultiplier;
                currentTorque *= modifiers.TorqueMultiplier;
                _rigid.drag = modifiers.Drag;
            }
            
            //If the player is not eliminated and the game is in play, apply movement
            if (!IsEliminated && GameLogic.Singleton.State != GameState.Waiting)
            {
                //Apply movement force to the player
                var forceDirection = worldInputVector * currentSpeed * Runner.DeltaTime;
                _rigid.AddForce(forceDirection, ForceMode.Force);

                //Apply torque rotation to the player
                var torqueDirection = new Vector3(inputData.vertical, 0, -inputData.horizontal).normalized *
                                      currentTorque *
                                      Runner.DeltaTime;
                _rigid.AddTorque(torqueDirection, ForceMode.Force);
            }
        }
    }

    //Method to set movement delay
    public void SetMovementDelay(float delay)
    {
        enableMovementTime = Time.time + delay;
    }

    //Client side method to play the elimination sound
    public void PlayEliminationSound()
    {
        if (_audioSource && eliminationSound)
        {
            _audioSource.PlayOneShot(eliminationSound);
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayCollisionSound() {
        if (_audioSource && collisionSound) {
            _audioSource.PlayOneShot(collisionSound);
        }
    }


    //RPC method to send names to all clients
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PlayerName(string name)
    {
        Name = name;
    }

    //RPC method to set readyness to all clients
    [Rpc(RpcSources.InputAuthority, RpcTargets.InputAuthority | RpcTargets.StateAuthority)]
    public void RPC_SetReady()
    {
        IsReady = true;
        if (HasInputAuthority)
        {
            UIManager.Singleton.DidSetReady();
        }
        Debug.Log($"RPC_SetReady: {Name} has hit ready");
    }

//RPC method to show elimination to all clients
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayerElimination()
    {
        IsEliminated = true;
        if (HasInputAuthority)
        {
            Debug.Log($"RPC_PlayerElimination: {Name} has been eliminated");
            StartCoroutine(DelayedCheckAndPlaySound());
        }
    }

    private IEnumerator DelayedCheckAndPlaySound()
    {
        // Wait for a short delay to ensure the game state has been updated across all clients
        yield return new WaitForSeconds(0.1f);
    
        // Check the game state after the delay and play sound if the game is still playing
        if (GameLogic.Singleton.State == GameState.Playing)
        {
            UIManager.Singleton.PlayerEliminated();
            PlayEliminationSound();
        }
    }

   
}