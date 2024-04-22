using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

//enum to store the two GameStates
public enum GameState
{
    Waiting,
    Playing
}

public class GameLogic : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    //Network object to show winner
    [Networked] private Player Winner { get; set; }
    
    //Network object to show game state
    [Networked] [OnChangedRender(nameof(GameStateChanged))] public GameState State { get; set; }
    
    //Dictionary to store all players in the game
    [Networked] [Capacity(12)] private NetworkDictionary<PlayerRef, Player> Players => default;
    
    [SerializeField] private NetworkPrefabRef playerPrefab; //Initialise player prefab
    [SerializeField] private List<Transform> spawnPoints; //List of spawnpoints in the scene
    
    private static GameLogic _singleton;
    public static GameLogic Singleton
    {
        get => _singleton;
        set
        {
            if (value == null)
            {
                _singleton = null;
            }
            else if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Destroy(value);
                Debug.LogError($"There should only ever be one instance of {nameof(GameLogic)}!");
            }
        }
    }

    private void Awake()
    {
        Singleton = this;
        FindSpawnPoints();
    }

    private void OnDestroy()
    {
        if (Singleton == this)
            Singleton = null;
    }
    
    //Called on Awake to find spawnpoints in the scene
    private void FindSpawnPoints()
    {
        spawnPoints = new List<Transform>();
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("Spawnpoint");
        foreach (var spawn in spawns)
        {
            spawnPoints.Add(spawn.transform);
        }
        Debug.Log($"Found {spawnPoints.Count} spawnpoints");
    }

    //Called when a player enters a trigger collider (exits arena)
    public void HandleArenaBoundaryTrigger(Collider other)
    {
        Debug.Log("Something entered collider");
        //Detects when a player enters the game arena collider and triggers an elimination
        if (Runner.IsServer && 
            State == GameState.Playing && 
            Winner == null && 
            other.attachedRigidbody != null &&
            other.attachedRigidbody.TryGetComponent(out Player player))
        {
            if (player.LastCollidedPlayer != default && Players.ContainsKey(player.LastCollidedPlayer))
            {
                Player scorer = Players[player.LastCollidedPlayer].GetComponent<Player>();
                if (scorer != null)
                {
                    scorer.Score++;
                }
            }
            Debug.Log($"{player} Eliminated");
            player.RPC_PlayerElimination();
        }
    }

    //Called when 2 player collide for sfx
    public void HandlePlayerCollision(Player player1, Player player2)
    {
        if (player1 == null || player2 == null) return;
        
        //Ensure only one of the two colliding players (the one with the smaller ID) handles the sound playing
        if (player1.Object.Id.CompareTo(player2.Object.Id) < 0) {
            //Check if it's time to play another sound to avoid spamming
            float currentTime = Time.time;
            if (currentTime > player1.lastSoundTime + player1.soundCooldown) {
                if (HasStateAuthority) 
                {
                    player1.RPC_PlayCollisionSound();  //Play sound via RPC
                }
                player1.lastSoundTime = currentTime;  //Update the last sound time
            }
        }
    }

    //Setting up player when they join the game
    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            //Find spawnpoint and place player there
            int spawnIndex = Players.Count % spawnPoints.Count;
            Transform spawnPoint = spawnPoints[spawnIndex];
            var position = spawnPoint.position;
            var rotation = spawnPoint.rotation;
            var playerObject = Runner.Spawn(playerPrefab, position, rotation, player);
            Players.Add(player, playerObject.GetComponent<Player>());
        }
    }

    //Removing player when they leave the game
    public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        if (Players.TryGet(player, out var playerBehaviour))
        {
            Players.Remove(player);
            Runner.Despawn(playerBehaviour.Object);
        }
    }


    public override void Spawned()
    {
        //Setting initial values when players are spawned
        Winner = null;
        State = GameState.Waiting;
        UIManager.Singleton.SetWaitUI(State, Winner);
        Runner.SetIsSimulated(Object, true);
    }

    public override void FixedUpdateNetwork()
    {
        if (Players.Count < 1)
            return;

        if (Runner.IsServer && State == GameState.Waiting)
        {
            //Continuously check if players are ready
            var areAllReady = true;
            foreach (var player in Players)
                if (!player.Value.IsReady)
                {
                    areAllReady = false;
                    break;
                }

            //If all players are ready to play, reset the game environment
            if (areAllReady)
            {
                Debug.Log("All players are ready. Starting the game.");
                Winner = null;              //Clear winner before starting game
                State = GameState.Playing;  //Set state to playing
            }
        }
        
        if (State == GameState.Playing)
            CheckForEndGame();
        
        // Update leaderboard with readiness or scores based on the current game state
        if (State == GameState.Waiting && !Runner.IsResimulation)
        {
            UIManager.Singleton.UpdateLeaderboard(Players.ToArray(), true); // Show readiness
        }
        else if (State == GameState.Playing && !Runner.IsResimulation)
        {
            UIManager.Singleton.UpdateLeaderboard(Players.OrderByDescending(p => p.Value.Score).ToArray(), false); // Show scores
        }
    }


    //Function called every time the GameState is changed
    private void GameStateChanged()
    {
        //Display waiting UI if gamestate changes (Called above in gamestate network object)
        Debug.Log($"GameState changed to {State}");
        UIManager.Singleton.SetWaitUI(State, Winner);

        if (State == GameState.Playing)
        {
            PreparePlayers();
            UIManager.Singleton.UpdateLeaderboard(Players.ToArray(), false); // Show Score
            AudioManager.Singleton.PlayStartSound();
            foreach (var player in Players)
            {
                player.Value.SetMovementDelay(2.6f);
            }
        } 
        else if (State == GameState.Waiting)
        {
            ResetPlayerStates();
            UIManager.Singleton.UpdateLeaderboard(Players.ToArray(), true); // Show readiness

        }
    }

    //Perform initial game setup at the start of match
    private void PreparePlayers()
    {
        int spawnIndex = 0;
        foreach (var player in Players)
        {
            Transform spawnPoint = spawnPoints[spawnIndex];
            var position = spawnPoint.position;
            var rotation = spawnPoint.rotation;

            Debug.Log($"Preparing {player.Value.Name}");
            TeleportPlayer(player.Value, position, rotation);
            player.Value.Score = 0; // Reset score
            player.Value.LastCollidedPlayer = default; // Reset collision tracking

            spawnIndex = (spawnIndex + 1) % spawnPoints.Count; // Ensure wrapping around the list
        }
    }

    //Teleporting players to their spawn points
    public void TeleportPlayer(Player player, Vector3 position, Quaternion rotation)
    {
        if (!Runner.IsServer)
            return;
        
        Debug.Log($"Teleporting {player.Name} from {player.transform.position} to {position}");
        player.Rigidbody.isKinematic = true;
        player.NetworkRigidbody3D.Teleport(position, rotation);
        player.Rigidbody.isKinematic = false;
    }
    
    //Reset players velocity and rotation
    private void ResetPlayerStates()
    {
        foreach (var player in Players)
        {
            player.Value.Rigidbody.isKinematic = false;
            player.Value.Rigidbody.velocity = Vector3.zero;
            player.Value.Rigidbody.angularVelocity = Vector3.zero;
        }
    }

    //Unready all players
    private void UnreadyAll()
    {
        foreach (var player in Players)
        {
            player.Value.IsEliminated = false;
            player.Value.IsReady = false;
        }
    }

    private void CheckForEndGame()
    {
        int activePlayers = 0;
        Player winner = null;

        foreach (var player in Players)
        {
            if (!player.Value.IsEliminated)
            {
                activePlayers++;
                winner = player.Value;
            }
        }

        if (activePlayers == 1)
        {
            Winner = winner; //Declare the winner
            AudioManager.Singleton.PlayWinnerSound();
            State = GameState.Waiting; //Set state to waiting
            UnreadyAll();
        }
    }
}