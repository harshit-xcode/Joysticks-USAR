using System.Collections;
using UnityEngine;

// Scalable universal block component that can execute any BlockType without requiring separate prefabs.
public class UniversalCodeBlock : CodeBlock
{
    public override IEnumerator Execute(PlayerController player)
    {
        if (player == null) yield break;

        switch (blockType)
        {
            case BlockType.Walk:
                yield return player.MoveForwardCoroutine();
                break;
            case BlockType.Lava:
                yield return player.JumpOverLavaCoroutine();
                break;
            case BlockType.MoveLeft:
                yield return player.MoveLeftCoroutine();
                break;
            case BlockType.MoveRight:
                yield return player.MoveRightCoroutine();
                break;
            case BlockType.Force:
                yield return player.ForceDashCoroutine();
                break;
            case BlockType.Wait:
                yield return player.WaitStepCoroutine();
                break;
            default:
                yield return null;
                break;
        }
    }
}
