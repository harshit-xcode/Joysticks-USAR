// RunButton.cs
// The ▶ RUN button in the coding interface.
// When clicked, it tells UIManager to collect the blocks from all slots
// and send them to CodeExecutor to be run.

using UnityEngine;
using UnityEngine.UI;

public class RunButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        // Bind the click event to OnRunClicked
        button.onClick.AddListener(OnRunClicked);
    }

    // Called when the player clicks the RUN button.
    public void OnRunClicked()
    {
        Debug.Log("RunButton: RUN clicked!");

        // Don't allow running while already running
        CodeExecutor executor = FindFirstObjectByType<CodeExecutor>();
        if (executor != null && executor.IsRunning())
        {
            Debug.LogWarning("RunButton: Code is already running.");
            return;
        }

        // Tell UIManager to collect blocks and start execution
        UIManager.Instance.CollectBlocksAndRun();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnRunClicked);
    }
}
