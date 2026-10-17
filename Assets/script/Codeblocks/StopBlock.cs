// StopBlock.cs
// The final block in every sequence. Signals that execution has ended.
// The robot stops moving and returns to an idle state.

using System.Collections;
using UnityEngine;

public class StopBlock : CodeBlock
{
    private void Awake()
    {
        blockType = BlockType.Stop;
    }

    // Signals the end of code execution.
    // Sets the player back to Idle state.
    public override IEnumerator Execute(PlayerController player)
    {
        Debug.Log("StopBlock: Execution finished.");
        player.currentState = PlayerState.Idle;
        yield return null; // Nothing to wait for
    }
}
