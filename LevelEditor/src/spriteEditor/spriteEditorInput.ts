import { KeyCode } from "../canvasUI";
import { GlobalInput } from "../globalInput";
import { Frame } from "../models/Frame";
import { Sprite } from "../models/Sprite";
import { Ghost, SpriteEditor } from "./spriteEditor";

export class SpriteEditorInput extends GlobalInput {

  spriteEditor: SpriteEditor;

  constructor(spriteEditor: SpriteEditor) {
    super();
    this.spriteEditor = spriteEditor;
  }

  onKeyDown(e: KeyboardEvent, keyCode: KeyCode, firstFrame: boolean) {
    if (keyCode === KeyCode.F1) {
      let helpString = 
`Ctrl+Z: Undo
Ctrl+Y: Redo
Ctrl+Mouse Wheel: zoom
F: Add Selected Frame
R: Replace Current Frame With Selected Frame
W/A/S/D: Move selected frame offset
Q/E: Go to previous/next frame
G: Activate Ghost mode
Escape: Exit Ghost mode
P: Place POI
H: Place Headshot POI
I: Place frame hitbox
`;
      window.Main.showDialog("Help", helpString);
      return;
    }

    if (firstFrame) {
      if (keyCode === KeyCode.E) {
        this.spriteEditor.selectNextFrame();
        e.preventDefault();
      } 
      else if (keyCode === KeyCode.Q) {
        this.spriteEditor.selectPrevFrame();
        e.preventDefault();
      }
      else if (keyCode === KeyCode.Z) {
        if (this.keysHeld.has(KeyCode.CONTROL)) {
          this.spriteEditor.undo();
          e.preventDefault();
        }
      }
      else if (keyCode === KeyCode.Y) {
        if (this.keysHeld.has(KeyCode.CONTROL)) {
          this.spriteEditor.redo();
          e.preventDefault();
        }
      }
    }
    
    let state = this.spriteEditor.data;

    if(keyCode === KeyCode.ESCAPE) {
      if (state.selectionId !== -1 || state.ghost !== undefined) {
        state.selectionId = -1;
        state.ghost = undefined;
        this.spriteEditor.changeState();
      }
    }

    if(state.selectedFrame && state.selectedSprite) {
      if(keyCode === KeyCode.G) {
        state.ghost = new Ghost(state.selectedSprite as Sprite, state.selectedFrame as Frame);
        this.spriteEditor.changeState();
      }
      else if(keyCode === KeyCode.H) {
        this.spriteEditor.changeAddPOIMode(true, "h");
      }
      else if(keyCode === KeyCode.P) {
        this.spriteEditor.changeAddPOIMode(true);
      }
      else if(keyCode === KeyCode.I) {
        this.spriteEditor.addHitboxToFrame(true);
      }
    }

    let moved = false;
    let moveFactor = this.spriteEditor.input.keysHeld.has(KeyCode.SHIFT) ? 10 : 1;
    if(state.selection && firstFrame) {
      if(keyCode === KeyCode.A) {
        state.selection.move(-1 * moveFactor, 0);
        moved = true;
      }
      else if(keyCode === KeyCode.D) {
        state.selection.move(1 * moveFactor, 0);
        moved = true;
      }
      else if(keyCode === KeyCode.W) {
        state.selection.move(0 * moveFactor, -1);
        moved = true;
      }
      else if(keyCode === KeyCode.S) {
        state.selection.move(0 * moveFactor, 1);
        moved = true;
      }
      else if(keyCode === KeyCode.LEFT) {
        state.selection.resizeCenter(-1 * moveFactor, 0);
        moved = true;
      }
      else if(keyCode === KeyCode.RIGHT) {
        state.selection.resizeCenter(1 * moveFactor, 0);
        moved = true;
      }
      else if(keyCode === KeyCode.DOWN) {
        state.selection.resizeCenter(0 * moveFactor, -1);
        moved = true;
      }
      else if(keyCode === KeyCode.UP) {
        state.selection.resizeCenter(0 * moveFactor, 1);
        moved = true;
      }
    }
    else if(state.selectedFrame && firstFrame) {
      if(keyCode === KeyCode.A) {
        state.selectedFrame.offset.x -= 1 * moveFactor;
        if (state.moveChildren) {
          state.selectedFrame.POIs.forEach(p => p.move(-moveFactor, 0));
          state.selectedFrame.hitboxes.forEach(h => h.move(-moveFactor, 0));
        }
        moved = true;
      }
      else if(keyCode === KeyCode.D) {
        state.selectedFrame.offset.x += 1 * moveFactor;
        if (state.moveChildren) {
          state.selectedFrame.POIs.forEach(p => p.move(moveFactor, 0));
          state.selectedFrame.hitboxes.forEach(p => p.move(moveFactor, 0));
        }
        moved = true;
      }
      else if(keyCode === KeyCode.W) {
        state.selectedFrame.offset.y -= 1 * moveFactor;
        if (state.moveChildren) {
          state.selectedFrame.POIs.forEach(p => p.move(0, -moveFactor));
          state.selectedFrame.hitboxes.forEach(p => p.move(0, -moveFactor));
        }
        moved = true;
      }
      else if(keyCode === KeyCode.S) {
        state.selectedFrame.offset.y += 1 * moveFactor;
        if (state.moveChildren) {
          state.selectedFrame.POIs.forEach(p => p.move(0, moveFactor));
          state.selectedFrame.hitboxes.forEach(p => p.move(0, moveFactor));
        }
        moved = true;
      }
    }

    if (moved) {
      state.setSelectedSpriteDirty(true);
      this.spriteEditor.changeState();
    }
  }

  onKeyUp(e: KeyboardEvent, keyCode: KeyCode) {
  }
  
}