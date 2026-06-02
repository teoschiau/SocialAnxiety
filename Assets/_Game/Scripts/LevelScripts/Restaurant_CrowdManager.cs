using UnityEngine;
using System.Collections.Generic;

public class Restaurant_CrowdManager : MonoBehaviour
{
    [Header("Main Actors (Trage-i aici din ierarhie)")]
    public GameObject waiterObject; 
    public GameObject friendObject; 
    public Transform crowdParent;   

    private List<GameObject> backgroundNPCs = new List<GameObject>();

    public void SetupScene(int difficulty, bool isCrowded)
    {
        IdentifyActors();

        if (waiterObject != null)
        {
            waiterObject.SetActive(true);
            SetWaiterPersona(difficulty); 
        }
        else Debug.LogError("CrowdManager: Nu ai asignat Waiter Object!");

        if (friendObject != null) friendObject.SetActive(true);
        ManageBackgroundCrowd(isCrowded);
    }

    void IdentifyActors()
    {
        backgroundNPCs.Clear();

        if (crowdParent != null)
        {
            foreach (Transform child in crowdParent)
            {
                GameObject obj = child.gameObject;

                if (obj == waiterObject || obj == friendObject)
                {
                    continue; 
                }

                backgroundNPCs.Add(obj);
                obj.SetActive(false); 
            }
        }
    }

    void SetWaiterPersona(int difficulty)
    {
        var robotScript = waiterObject.GetComponent<RobotConversation>();
        
        if (robotScript != null)
        {
            robotScript.chatProbability = 100;

            switch (difficulty)
            {
                case 1: // Easy
                    robotScript.myPersona = "You are a very polite waiter. You realize you made a mistake with the order and apologize profusely to the customer.";
                    break;
                case 2: // Medium
                    robotScript.myPersona = "You are a busy waiter. You believe the customer ordered this dish, but you are willing to double-check with the kitchen if they insist politely.";
                    break;
                case 3: // Hard
                    robotScript.myPersona = "You are an arrogant waiter. You are convinced you never make mistakes. The customer is probably wrong. You require strong arguments.";
                    break;
                default:
                    robotScript.myPersona = "You are a waiter taking an order.";
                    break;
            }
            
            Debug.Log($"CrowdManager: Persona ospatarului a fost setata pentru dificultatea {difficulty}.");
        }
        else
        {
            Debug.LogError("CrowdManager: Obiectul Ospatar NU are scriptul 'RobotConversation' pe el!");
        }
    }

    void ManageBackgroundCrowd(bool isCrowded)
    {
        for (int i = 0; i < backgroundNPCs.Count; i++)
        {
            GameObject temp = backgroundNPCs[i];
            int randomIndex = Random.Range(i, backgroundNPCs.Count);
            backgroundNPCs[i] = backgroundNPCs[randomIndex];
            backgroundNPCs[randomIndex] = temp;
        }
        int countToActivate = isCrowded ? 20 : 2;

        for (int i = 0; i < backgroundNPCs.Count; i++)
        {
            if (i < countToActivate) backgroundNPCs[i].SetActive(true);
            else backgroundNPCs[i].SetActive(false);
        }
    }
}