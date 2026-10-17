using System;
using System.Collections.Generic;

// The player edits the connected nodes between START and END.
public static class ProgramSequence
{
    public static bool IsBoundary(BlockType type)
    {
        return type == BlockType.Start || type == BlockType.Stop;
    }

    public static bool IsEditableCommand(BlockType type)
    {
        return !IsBoundary(type) && Enum.IsDefined(typeof(BlockType), type);
    }

    public static List<BlockType> WithFixedEndpoints(IEnumerable<BlockType> body)
    {
        if (body == null) throw new ArgumentNullException(nameof(body));
        var commands = new List<BlockType> { BlockType.Start };
        foreach (var type in body)
        {
            if (IsBoundary(type)) continue;
            if (!IsEditableCommand(type))
                throw new ArgumentException("Unknown editable command: " + type, nameof(body));
            commands.Add(type);
        }
        commands.Add(BlockType.Stop);
        return commands;
    }
}
