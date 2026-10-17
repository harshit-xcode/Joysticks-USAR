using System.Collections;
using UnityEngine;

public class WaitBlock : CodeBlock
{
    private void Awake()
    {
        blockType = BlockType.Wait;
    }

    public override IEnumerator Execute(PlayerController player)
    {
        yield return player.WaitStepCoroutine();
    }
}
