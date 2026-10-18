using UnityEngine;

// A snapshot captured before respawn resets the robot and environment.
public sealed class ExecutionFailure
{
    public int Pass { get; }
    public int CommandIndex { get; }
    public BlockType Command { get; }
    public Vector2Int Coordinate { get; }
    public int Beat { get; } // One-based for the player; TileManager's internal clock is zero-based.
    public string Reason { get; }

    public ExecutionFailure(int pass, int commandIndex, BlockType command, Vector2Int coordinate, int beat, string reason)
    {
        Pass = pass;
        CommandIndex = commandIndex;
        Command = command;
        Coordinate = coordinate;
        Beat = beat;
        Reason = reason;
    }

    public string Summary => $"Pass {Pass}, Node {CommandIndex + 1}, Beat {Beat}: " +
        $"{CommandLabels.For(Command)} {Reason} at ({Coordinate.x},{Coordinate.y}).";

}
