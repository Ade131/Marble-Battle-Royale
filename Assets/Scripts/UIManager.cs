using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private static UIManager _singleton;

    //Text UI fields for instructions and messages
    [SerializeField] private TextMeshProUGUI gameStateText;
    [SerializeField] private TextMeshProUGUI instructionText;
    // Text fields for eliminations
    [SerializeField] private TextMeshProUGUI eliminationText;
    [SerializeField] private TextMeshProUGUI eliminationInstructionText;
    //Array for leaderboard items
    [SerializeField] private LeaderboardItem[] leaderboardItems;

    public static UIManager Singleton
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
                Debug.LogError($"There should only ever be one instance of {nameof(UIManager)}!");
            }
        }
    }

    private void Awake()
    {
        Singleton = this;
    }

    private void OnDestroy()
    {
        if (Singleton == this)
            Singleton = null;
    }

    public void DidSetReady()
    {
        instructionText.text = "Waiting for other players to ready up...";
    }

    public void SetWaitUI(GameState newState, Player winner)
    {
        //While the game is waiting, display instructions
        if (newState == GameState.Waiting)
        {
            if (winner == null)
            {
                gameStateText.text = "Waiting to Start";
                instructionText.text = "Press R when you're ready to begin!";
            }
            else
            {
                //Display winners name if there is a winner
                gameStateText.text = $"{winner.Name} Wins!";
                instructionText.text = "Press R when you're ready to begin!";
            }
        }
        //Hide elimination text
        eliminationText.enabled = false;
        eliminationInstructionText.enabled = false;
        
        //Show waiting/winner text
        gameStateText.enabled = newState == GameState.Waiting;
        instructionText.enabled = newState == GameState.Waiting;
        
        
    }

    //Show text when player is eliminated
    public void PlayerEliminated()
    {
        Debug.Log("Player elimination UI update triggered.");
        eliminationText.text = "Eliminated!";
        eliminationInstructionText.text = "Please wait for the game to end.";
        eliminationText.enabled = true;
        eliminationInstructionText.enabled = true;
    }

    public void UpdateLeaderboard(KeyValuePair<Fusion.PlayerRef, Player>[] players, bool showReadiness)
    {
        for (int i = 0; i < leaderboardItems.Length; i++)
        {
            LeaderboardItem item = leaderboardItems[i];
            if (i < players.Length)
            {
                item.nameText.text = players[i].Value.Name;
                if (showReadiness)
                {
                    // During the 'Waiting' state, show whether the player is ready
                    item.altText.text = players[i].Value.IsReady ? "Ready" : "Waiting...";
                }
                else
                {
                    // During the 'Playing' state, show 'X' if eliminated, otherwise show the score
                    item.altText.text = players[i].Value.IsEliminated ? "X" : players[i].Value.Score.ToString();
                }
            }
            else
            {
                //Fills with empty values in the list for extra players
                item.nameText.text = "";
                item.altText.text = "";
            }
        }
    }

    [Serializable]
    private struct LeaderboardItem
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI altText;
    }
}