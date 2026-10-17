using System;
using System.Collections.Generic;

// The player edits only the body. Exactly one START and STOP wrap every program.
public static class ProgramSequence
{
    public static bool IsBoundary(BlockType type) { return type == BlockType.Start || type == BlockType.Stop; }

    public static List<BlockType> WithFixedEndpoints(IEnumerable<BlockType> body)
    {
        if (body == null) throw new ArgumentNullException(nameof(body));
        var commands = new List<BlockType> { BlockType.Start };
        foreach (var type in body)
        {
            if (IsBoundary(type)) continue;
            if (type != BlockType.Walk && type != BlockType.Lava)
                throw new ArgumentException("Unknown editable command.", nameof(body));
            commands.Add(type);
        }
        commands.Add(BlockType.Stop);
        return commands;
    }
}
