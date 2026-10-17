// StartBlock.cs
// The first block in every sequence. Signals that execution has begun.
// The robot "wakes up" and gets ready to move.

using System.Collections;
using UnityEngine;

public class StartBlock : CodeBlock
{
    private void Awake()
    {
        blockType = BlockType.Start;
    }

    // Signals the start of code execution.
    // No movement happens here — just sets the player to Idle/ready state.
    public override IEnumerator Execute(PlayerController player)
    {
        Debug.Log("StartBlock: Execution started.");
        player.currentState = PlayerState.Idle;
        yield return null; // Nothing to wait for
    }
}
