using System.Collections;
using UnityEngine;

public class ForceBlock : CodeBlock
{
    private void Awake()
    {
        blockType = BlockType.Force;
    }

    public override IEnumerator Execute(PlayerController player)
    {
        yield return player.ForceDashCoroutine();
    }
}
