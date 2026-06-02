using UnityEngine;
using System.Collections.Generic;

public class Bus_CrowdManager : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The parent object that holds all the sitting NPCs as children.")]
    public Transform crowdParent;
    public GameObject busDriver;

    private List<GameObject> allAudienceMembers = new List<GameObject>();

    void Awake()
    {
        if (crowdParent != null)
        {
            foreach (Transform child in crowdParent)
            {
                allAudienceMembers.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("Bus_CrowdManager: Crowd Parent is not assigned!");
        }
    }

    public void SetupAudience(int count)
    {
        ShuffleList(allAudienceMembers);

        int activeCount = 0;

        for (int i = 0; i < allAudienceMembers.Count; i++)
        {
            if (activeCount < count-1)
            {
                allAudienceMembers[i].SetActive(true);
                activeCount++;
            }
            else
            {
                allAudienceMembers[i].SetActive(false);
            }
        }
        if (busDriver != null)
        {
            busDriver.SetActive(true);
        }


    }

    void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

}
