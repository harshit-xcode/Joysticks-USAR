using System;
using System.Collections.Generic;

namespace Loops
{
    public enum CommandKind { Move, TurnLeft, TurnRight, Jump, ClearObstacle, Repeat }

    [Serializable]
    public sealed class ProgramBlock
    {
        public CommandKind kind;
        public int repetitions = 2;
        public List<ProgramBlock> children = new List<ProgramBlock>();

        public ProgramBlock(CommandKind kind) { this.kind = kind; }

        public bool Contains(ProgramBlock other)
        {
            if (ReferenceEquals(this, other)) return true;
            foreach (var child in children)
                if (child.Contains(other)) return true;
            return false;
        }
    }

    public static class ProgramCompiler
    {
        public const int MaxDepth = 8;
        public const int MaxRepeat = 20;
        public const int MaxInstructions = 512;

        // Compile first so an invalid or excessive program never partially runs.
        public static List<ProgramBlock> Compile(IReadOnlyList<ProgramBlock> blocks)
        {
            var result = new List<ProgramBlock>();
            var visiting = new HashSet<ProgramBlock>();
            int visited = 0;
            Expand(blocks, result, visiting, 0, ref visited);
            if (result.Count == 0) throw new ArgumentException("Add at least one robot command.");
            return result;
        }

        static void Expand(IReadOnlyList<ProgramBlock> blocks, List<ProgramBlock> result,
            HashSet<ProgramBlock> visiting, int depth, ref int visited)
        {
            if (blocks == null) throw new ArgumentException("A command list is missing.");
            if (depth > MaxDepth) throw new ArgumentException("Use no more than 8 nested repeats.");
            foreach (var block in blocks)
            {
                if (++visited > 20000) throw new ArgumentException("Program is too complex.");
                if (block == null || !visiting.Add(block))
                    throw new ArgumentException("Program contains a missing block or circular repeat.");
                if (!Enum.IsDefined(typeof(CommandKind), block.kind))
                    throw new ArgumentException("Unknown command.");
                if (block.kind == CommandKind.Repeat)
                {
                    if (block.repetitions < 1 || block.repetitions > MaxRepeat)
                        throw new ArgumentException("Repeat count must be between 1 and 20.");
                    if (block.children == null || block.children.Count == 0)
                        throw new ArgumentException("Put commands inside every repeat block.");
                    for (int i = 0; i < block.repetitions; i++)
                        Expand(block.children, result, visiting, depth + 1, ref visited);
                }
                else
                {
                    if (block.children != null && block.children.Count != 0)
                        throw new ArgumentException("Only repeat blocks can contain commands.");
                    if (result.Count >= MaxInstructions)
                        throw new ArgumentException("Program expands beyond 512 commands. Use a shorter route.");
                    result.Add(block);
                }
                visiting.Remove(block);
            }
        }
    }
}
