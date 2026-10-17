// LavaBlock.cs
// Safely crosses a lava tile without triggering a respawn.
// Sets the isCrossingLava flag on PlayerController to true before moving,
// so LavaTile knows this crossing is intentional and safe.
// After crossing, the flag is reset to false.

using System.Collections;
using UnityEngine;

public class LavaBlock : CodeBlock
{
    private void Awake()
    {
        blockType = BlockType.Lava;
    }

    // Crosses one tile safely even if it is a lava tile.
    // Uses JumpOverLavaCoroutine which sets/resets the lava crossing flag.
    public override IEnumerator Execute(PlayerController player)
    {
        Debug.Log("LavaBlock: Crossing lava tile safely.");
        yield return player.JumpOverLavaCoroutine();
    }
}
