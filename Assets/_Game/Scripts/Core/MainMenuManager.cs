using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Make sure these match your Scene filenames EXACTLY!
    [Header("Scene Names")]
    public string sceneStreet = "Level_Street";
    public string sceneRestaurant = "Level_Restaurant";
    public string sceneBus = "Level_Bus";
    public string scenePresentation = "Level_Presentation";

    // --- PUBLIC HELPER FUNCTIONS ---
    // (These will now appear in the dropdown menu!)

    public void LoadStreet()
    {
        LoadLevel(sceneStreet);
    }

    public void LoadRestaurant()
    {
        LoadLevel(sceneRestaurant);
    }

    public void LoadBus()
    {
        LoadLevel(sceneBus);
    }

    public void LoadPresentation()
    {
        LoadLevel(scenePresentation);
    }

    // Internal logic
    private void LoadLevel(string sceneName)
    {
        Debug.Log("Loading: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}