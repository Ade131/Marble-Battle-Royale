using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerHandler : MonoBehaviour
{
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Something entered collider");
            if (GameLogic.Singleton != null)
            {
                GameLogic.Singleton.HandleArenaBoundaryTrigger(other);
            }
            else
            {
                Debug.LogError("GameLogic instance not found!");
            }
        }
    }
