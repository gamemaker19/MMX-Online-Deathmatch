import { GlobalInput } from "../globalInput";
import { KeyCode } from "../canvasUI";
import { LevelEditor } from "./levelEditor";
import { SelectTool } from "./tool";

export class LevelEditorInput extends GlobalInput {

  levelEditor: LevelEditor;

  constructor(levelEditor: LevelEditor) {
    super();
    this.levelEditor = levelEditor;
  }

  /*
  $(window).bind('mousewheel DOMMouseScroll', function (event) {
    if (event.ctrlKey == true) {
      event.preventDefault();
    }
  });
  */

  onKeyDown(e: KeyboardEvent, keyCode: KeyCode, firstFrame: boolean) {
    let data = this.levelEditor.data;

    if (this.levelEditor.tool) {
      this.levelEditor.tool.onKeyDown(keyCode, firstFrame);
    }

    if (firstFrame) {
      if (keyCode === KeyCode.Z) {
        if (this.keysHeld.has(KeyCode.CONTROL)) {
          this.levelEditor.undo();
          return;
        }
      }
      else if (keyCode === KeyCode.Y) {
        if (this.keysHeld.has(KeyCode.CONTROL)) {
          this.levelEditor.redo();
          return;
        }
      }

      if (keyCode === KeyCode.F1) {
        let helpString = 
`
GENERAL:
Ctrl+Z: Undo
Ctrl+Y: Redo
Ctrl+Mouse Wheel: zoom
Space: place last object
C: copy selected shape(s)
B: show camera bounds

SELECTED SHAPE INSTANCE:
WASD: resize left/top of shape
Shift + WASD: resize right/bottom of shape
Arrow Keys: move shape
Tab: enter/exit vertex mode
E (Collision Shapes only): snap nearby vertices

SELECTED NON-SHAPE INSTANCE:
WASD: move instance(s)

SELECTED TWO NODES:
F: link two nodes
V: link two nodes (one way)
X: unlink two nodes
R: insert node between two nodes`;
        window.Main.showDialog("Help", helpString);
      }
    }
  }

  onKeyUp(e: KeyboardEvent, keyCode: KeyCode) {
    if (this.levelEditor.tool) this.levelEditor.tool.onKeyUp(keyCode);
    this.levelEditor.redraw();
  }
}