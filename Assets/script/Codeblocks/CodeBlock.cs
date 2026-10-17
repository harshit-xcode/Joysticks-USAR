// CodeBlock.cs
// Abstract base class for all coding blocks in the Scratch-like interface.
// Every block (StartBlock, WalkBlock, LavaBlock, StopBlock) inherits from this.
// Each block implements Execute() as a coroutine so the CodeExecutor
// can wait for it to finish before running the next block.

using System.Collections;
using UnityEngine;

public enum BlockType
{
    Start,
    Walk,
    Lava,
    Stop
}

public abstract class CodeBlock : MonoBehaviour
{
    [Header("Block Info")]
    public BlockType blockType;   // What kind of block this is

    // Executes this block's action on the player.
    // Must be a coroutine so that movement/animations can be awaited.
    public abstract IEnumerator Execute(PlayerController player);
}
