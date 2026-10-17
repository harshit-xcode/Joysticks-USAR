using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Loops
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class BlockProgramUI : MonoBehaviour
    {
        public RobotProgramRunner runner;
        readonly List<ProgramBlock> program = new List<ProgramBlock>();
        readonly List<DropZone> zones = new List<DropZone>();
        readonly Dictionary<ProgramBlock, VisualElement> headers = new Dictionary<ProgramBlock, VisualElement>();
        List<ProgramBlock> selected;
        VisualElement root, editor, workspace, ghost;
        ScrollView scroll;
        Label status;
        Button run, stop;
        ProgramBlock dragged;
        List<ProgramBlock> dragSource;
        Vector2 dragStart;
        int pointer = -1;
        bool moved;
        string SaveKey => "Loops.Program." + SceneManager.GetActiveScene().path;

        [Serializable] sealed class SavedProgram { public List<ProgramBlock> blocks; }
        sealed class DropZone
        {
            public VisualElement element;
            public List<ProgramBlock> blocks;
            public ProgramBlock owner;
            public readonly List<VisualElement> rows = new List<VisualElement>();
        }

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            root.style.flexGrow = 1;
            root.RegisterCallback<PointerMoveEvent>(OnDragMove);
            root.RegisterCallback<PointerUpEvent>(OnDragEnd);
            root.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            root.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            selected = program;
            Build();
            if (runner != null) runner.Changed += RefreshState;
            RefreshState();
        }

        void OnDisable()
        {
            if (runner != null) runner.Changed -= RefreshState;
            CancelDrag();
            if (root == null) return;
            root.UnregisterCallback<PointerMoveEvent>(OnDragMove);
            root.UnregisterCallback<PointerUpEvent>(OnDragEnd);
            root.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            root.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            root.Clear();
        }

        void Build()
        {
            var panel = new VisualElement();
            panel.style.width = 390;
            panel.style.maxWidth = Length.Percent(42);
            panel.style.height = Length.Percent(100);
            panel.style.paddingLeft = panel.style.paddingRight = 14;
            panel.style.paddingTop = panel.style.paddingBottom = 14;
            panel.style.backgroundColor = new Color(.055f, .075f, .12f, .98f);
            panel.style.color = Color.white;
            root.Add(panel);
            var title = new Label("LOOPS / ROBOT LAB");
            title.style.fontSize = 21;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            var help = new Label("Drag blocks by their grip. Drop inside Repeat to nest.\nOr select a list, then click a command to add it.");
            help.style.whiteSpace = WhiteSpace.Normal;
            help.style.marginTop = help.style.marginBottom = 10;
            panel.Add(help);

            editor = new VisualElement();
            editor.style.flexGrow = 1;
            panel.Add(editor);
            var palette = new VisualElement();
            palette.style.flexDirection = FlexDirection.Row;
            palette.style.flexWrap = Wrap.Wrap;
            editor.Add(palette);
            foreach (CommandKind kind in Enum.GetValues(typeof(CommandKind)))
            {
                var command = kind;
                var chip = new Label("+ " + Title(kind));
                chip.style.paddingLeft = chip.style.paddingRight = 9;
                chip.style.paddingTop = chip.style.paddingBottom = 8;
                chip.style.marginRight = chip.style.marginBottom = 5;
                chip.style.backgroundColor = ColorFor(kind);
                chip.style.color = Color.white;
                chip.RegisterCallback<PointerDownEvent>(evt => StartDrag(evt, new ProgramBlock(command), null));
                palette.Add(chip);
            }
            var top = new VisualElement();
            top.style.flexDirection = FlexDirection.Row;
            top.Add(MakeButton("Main program", () => { selected = program; RenderProgram(); }));
            top.Add(MakeButton("Clear all", () => { program.Clear(); selected = program; RenderProgram(); }));
            editor.Add(top);
            scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.minHeight = 100;
            editor.Add(scroll);
            workspace = new VisualElement();
            scroll.Add(workspace);
            var saves = new VisualElement();
            saves.style.flexDirection = FlexDirection.Row;
            saves.Add(MakeButton("Save program", Save));
            saves.Add(MakeButton("Load saved", Load));
            editor.Add(saves);

            var controls = new VisualElement();
            controls.style.flexDirection = FlexDirection.Row;
            run = MakeButton("Run", () => runner?.Run(program));
            stop = MakeButton("Stop", () => runner?.StopProgram());
            controls.Add(run);
            controls.Add(stop);
            controls.Add(MakeButton("Reset level", () => runner?.ResetLevel()));
            panel.Add(controls);
            status = new Label();
            status.style.whiteSpace = WhiteSpace.Normal;
            status.style.minHeight = 62;
            status.style.marginTop = 8;
            panel.Add(status);
            var rules = new Label("Move: 1 tile  |  Jump: over 1 lava tile\nClear: remove the tree/log directly ahead\nBlue tile = goal. Robot's face points forward.");
            rules.style.whiteSpace = WhiteSpace.Normal;
            rules.style.fontSize = 11;
            rules.style.color = new Color(.65f, .75f, .85f);
            panel.Add(rules);
            RenderProgram();
        }

        void RenderProgram()
        {
            workspace.Clear();
            zones.Clear();
            headers.Clear();
            DrawList(workspace, program, null);
            RefreshState();
        }

        void DrawList(VisualElement parent, List<ProgramBlock> blocks, ProgramBlock owner)
        {
            var body = new VisualElement();
            body.style.paddingLeft = owner == null ? 3 : 12;
            body.style.paddingRight = 3;
            body.style.paddingTop = body.style.paddingBottom = 8;
            body.style.minHeight = 44;
            body.style.borderLeftWidth = 3;
            body.style.borderLeftColor = ReferenceEquals(selected, blocks) ? Color.cyan : new Color(.24f, .3f, .4f);
            parent.Add(body);
            var zone = new DropZone { element = body, blocks = blocks, owner = owner };
            zones.Add(zone);
            body.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target != body) return;
                selected = blocks;
                RenderProgram();
                evt.StopPropagation();
            });
            if (blocks.Count == 0)
            {
                var hint = new Label(owner == null ? "Drop commands here" : "Drop repeat commands here");
                hint.style.color = new Color(.6f, .7f, .8f);
                hint.pickingMode = PickingMode.Ignore;
                body.Add(hint);
            }
            foreach (var block in blocks)
            {
                var row = new VisualElement();
                row.style.marginBottom = 5;
                row.style.backgroundColor = new Color(.1f, .13f, .19f);
                body.Add(row);
                zone.rows.Add(row);
                var head = new VisualElement();
                head.style.flexDirection = FlexDirection.Row;
                head.style.alignItems = Align.Center;
                head.style.minHeight = 34;
                head.style.backgroundColor = ColorFor(block.kind);
                row.Add(head);
                headers.Add(block, head);
                var grip = new Label(":: " + Title(block.kind));
                grip.style.paddingLeft = 8;
                grip.style.flexGrow = 1;
                grip.style.unityFontStyleAndWeight = FontStyle.Bold;
                grip.RegisterCallback<PointerDownEvent>(evt => StartDrag(evt, block, blocks));
                head.Add(grip);
                if (block.kind == CommandKind.Repeat)
                {
                    head.Add(MakeButton("-", () => { block.repetitions = Mathf.Max(1, block.repetitions - 1); RenderProgram(); }));
                    head.Add(new Label(block.repetitions.ToString()));
                    head.Add(MakeButton("+", () => { block.repetitions = Mathf.Min(20, block.repetitions + 1); RenderProgram(); }));
                    head.Add(MakeButton("Inside", () => { selected = block.children; RenderProgram(); }));
                }
                head.Add(MakeButton("x", () =>
                {
                    // A selected nested list may be part of the subtree being removed.
                    selected = program;
                    blocks.Remove(block);
                    RenderProgram();
                }));
                if (block.kind == CommandKind.Repeat) DrawList(row, block.children, block);
            }
        }

        void StartDrag(PointerDownEvent evt, ProgramBlock block, List<ProgramBlock> source)
        {
            if (evt.button != 0 || pointer >= 0 || (runner != null && runner.IsRunning)) return;
            dragged = block;
            dragSource = source;
            dragStart = evt.position;
            pointer = evt.pointerId;
            moved = false;
            root.CapturePointer(pointer);
            evt.StopPropagation();
        }

        void OnDragMove(PointerMoveEvent evt)
        {
            if (pointer != evt.pointerId || dragged == null) return;
            if (!moved && Vector2.Distance(dragStart, evt.position) < 7) return;
            moved = true;
            if (ghost == null)
            {
                ghost = new Label(Title(dragged.kind));
                ghost.pickingMode = PickingMode.Ignore;
                ghost.style.position = Position.Absolute;
                ghost.style.backgroundColor = ColorFor(dragged.kind);
                ghost.style.color = Color.white;
                ghost.style.paddingLeft = ghost.style.paddingRight = 12;
                ghost.style.paddingTop = ghost.style.paddingBottom = 8;
                root.Add(ghost);
            }
            Vector2 local = root.WorldToLocal(evt.position);
            ghost.style.left = local.x + 14;
            ghost.style.top = local.y + 10;
            // Allow long programs to scroll while dragging near the viewport edges.
            var bounds = scroll.contentViewport.worldBound;
            if (bounds.Contains(evt.position))
            {
                if (evt.position.y < bounds.yMin + 28) scroll.scrollOffset -= new Vector2(0, 12);
                if (evt.position.y > bounds.yMax - 28) scroll.scrollOffset += new Vector2(0, 12);
            }
        }

        void OnDragEnd(PointerUpEvent evt)
        {
            if (pointer != evt.pointerId || dragged == null) return;
            var block = dragged;
            var source = dragSource;
            bool didMove = moved;
            DropZone target = null;
            if (didMove && scroll.contentViewport.worldBound.Contains(evt.position))
                foreach (var zone in zones)
                    if (zone.element.worldBound.Contains(evt.position)) target = zone;
            int index = 0;
            if (target != null)
                while (index < target.rows.Count && evt.position.y > target.rows[index].worldBound.center.y) index++;
            CancelDrag();
            if (!didMove)
            {
                if (source == null) selected.Add(block);
                else selected = block.kind == CommandKind.Repeat ? block.children : source;
            }
            else if (target != null)
            {
                if (target.owner != null && block.Contains(target.owner))
                { status.text = "A repeat cannot be placed inside itself."; return; }
                if (source != null)
                {
                    int previous = source.IndexOf(block);
                    if (ReferenceEquals(source, target.blocks) && previous < index) index--;
                    source.Remove(block);
                }
                target.blocks.Insert(Mathf.Clamp(index, 0, target.blocks.Count), block);
                selected = target.blocks;
            }
            RenderProgram();
        }

        void OnPointerCancel(PointerCancelEvent evt) { if (evt.pointerId == pointer) CancelDrag(); }
        void OnCaptureOut(PointerCaptureOutEvent evt) { if (evt.pointerId == pointer) CancelDrag(); }

        void CancelDrag()
        {
            int oldPointer = pointer;
            pointer = -1;
            dragged = null;
            dragSource = null;
            ghost?.RemoveFromHierarchy();
            ghost = null;
            if (root != null && oldPointer >= 0 && root.HasPointerCapture(oldPointer)) root.ReleasePointer(oldPointer);
        }

        void RefreshState()
        {
            if (status == null) return;
            bool running = runner != null && runner.IsRunning;
            editor.SetEnabled(!running);
            run.SetEnabled(runner != null && !running);
            stop.SetEnabled(running);
            status.text = runner != null ? runner.Status : "Assign a RobotProgramRunner in the Inspector.";
            foreach (var entry in headers)
                entry.Value.style.backgroundColor = runner != null && ReferenceEquals(entry.Key, runner.ActiveBlock)
                    ? new Color(.1f, .65f, .45f) : ColorFor(entry.Key.kind);
        }

        void Save()
        {
            try
            {
                ProgramCompiler.Compile(program);
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new SavedProgram { blocks = program }));
                PlayerPrefs.Save();
                status.text = "Program saved on this device for this scene.";
            }
            catch (ArgumentException error) { status.text = error.Message; }
        }

        void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) { status.text = "No program saved for this scene yet."; return; }
            try
            {
                var saved = JsonUtility.FromJson<SavedProgram>(PlayerPrefs.GetString(SaveKey));
                ProgramCompiler.Compile(saved?.blocks);
                program.Clear();
                program.AddRange(saved.blocks);
                selected = program;
                RenderProgram();
                status.text = "Saved program loaded.";
            }
            catch (ArgumentException error) { status.text = "Could not load: " + error.Message; }
        }

        static Button MakeButton(string text, Action action)
        {
            var button = new Button(action) { text = text };
            button.style.minHeight = 28;
            button.style.marginRight = 3;
            return button;
        }

        static string Title(CommandKind kind)
        {
            switch (kind)
            {
                case CommandKind.Move: return "Move";
                case CommandKind.TurnLeft: return "Turn left";
                case CommandKind.TurnRight: return "Turn right";
                case CommandKind.Jump: return "Jump lava";
                case CommandKind.ClearObstacle: return "Clear obstacle";
                default: return "Repeat";
            }
        }

        static Color ColorFor(CommandKind kind)
        {
            return kind == CommandKind.Repeat ? new Color(.65f, .34f, .06f) :
                kind == CommandKind.ClearObstacle ? new Color(.45f, .25f, .65f) :
                kind == CommandKind.Jump ? new Color(.7f, .22f, .1f) : new Color(.12f, .33f, .64f);
        }
    }
}
