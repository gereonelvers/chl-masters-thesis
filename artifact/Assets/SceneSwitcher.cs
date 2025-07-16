using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // Import Photon PUN2
using Photon.Realtime;

public class NetworkedSceneSwitcher : MonoBehaviourPunCallbacks
{
    /// <summary>
    /// The name of the home scene to return to.
    /// Set this in the Inspector or directly in the script.
    /// </summary>
    [SerializeField]
    private string homeSceneName = "HomeScene"; // Replace with your actual home scene name

    /// <summary>
    /// Stack to keep track of scene history for switching back.
    /// Note: This history is local. If you need a synchronized history, you'll need extra handling.
    /// </summary>
    private Stack<string> sceneHistory = new Stack<string>();

    private void Start()
    {
        // Ensure that all clients automatically load the same scene as the Master Client.
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    /// <summary>
    /// Switches to the next scene in the build settings list.
    /// If the current scene is the last one, it loops back to the first scene.
    /// Only the Master Client is allowed to switch scenes.
    /// </summary>
    public void SwitchToNextScene()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only the Master Client can switch scenes.");
            return;
        }

        // Get the current active scene's build index (using UnityEngine.SceneManagement)
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        // If the current scene is the last one, loop back to the first scene
        if (nextSceneIndex >= UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            nextSceneIndex = 0;
        }

        // Push the current scene name onto history before switching
        sceneHistory.Push(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        // Load the next scene across the network
        PhotonNetwork.LoadLevel(nextSceneIndex);
    }

    /// <summary>
    /// Switches to a specific scene by name.
    /// Only the Master Client is allowed to switch scenes.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public void SwitchToScene(string sceneName)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only the Master Client can switch scenes.");
            return;
        }

        // Check if the scene is in the Build Settings.
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            // Save the current scene's name
            sceneHistory.Push(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

            // Load the scene across the network
            PhotonNetwork.LoadLevel(sceneName);
        }
        else
        {
            Debug.LogError($"Scene '{sceneName}' cannot be loaded. Ensure it's added to Build Settings.");
        }
    }

    /// <summary>
    /// Returns to the home scene specified by 'homeSceneName'.
    /// Clears the scene history.
    /// Only the Master Client is allowed to switch scenes.
    /// </summary>
    public void ReturnHome()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only the Master Client can switch scenes.");
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(homeSceneName))
        {
            // Clear the history since we're returning home
            sceneHistory.Clear();

            // Load the home scene across the network
            PhotonNetwork.LoadLevel(homeSceneName);
        }
        else
        {
            Debug.LogError($"Home scene '{homeSceneName}' cannot be loaded. Ensure it's added to Build Settings.");
        }
    }

    /// <summary>
    /// Switches back to the previous scene in the history.
    /// Only the Master Client is allowed to switch scenes.
    /// </summary>
    public void SwitchToPreviousScene()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only the Master Client can switch scenes.");
            return;
        }

        if (sceneHistory.Count > 0)
        {
            // Pop the last scene name from the history
            string previousSceneName = sceneHistory.Pop();

            // Load the previous scene across the network
            PhotonNetwork.LoadLevel(previousSceneName);
        }
        else
        {
            Debug.LogWarning("No previous scene to switch to.");
        }
    }

    /// <summary>
    /// Optional: Clear scene history (e.g., when starting a new game)
    /// </summary>
    public void ClearSceneHistory()
    {
        sceneHistory.Clear();
    }
}
