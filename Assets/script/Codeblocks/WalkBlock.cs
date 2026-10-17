// WalkBlock.cs
// Moves the player one tile forward when executed.
// The player smoothly walks to the next tile via PlayerController.MoveForwardCoroutine().

using System.Collections;
using UnityEngine;

public class WalkBlock : CodeBlock
{
    private void Awake()
    {
        blockType = BlockType.Walk;
    }

    // Moves the player one tile forward.
    // Waits until the movement is fully complete before finishing.
    public override IEnumerator Execute(PlayerController player)
    {
        Debug.Log("WalkBlock: Moving player forward one tile.");
        yield return player.MoveForwardCoroutine();
    }
}
