import { KeyCode } from "./canvasUI";

export class GlobalInput {

  keysHeld: Set<KeyCode> = new Set();

  constructor() {
    document.onkeydown = (e: KeyboardEvent) => {
      if(document.activeElement && this.isTextBox(document.activeElement)) {
        return;
      }
      let keyCode = <KeyCode>e.keyCode;
      this.onKeyDown(e, keyCode, !this.keysHeld.has(keyCode));
      this.keysHeld.add(keyCode);
      // e.preventDefault();
    }
    
    document.onkeyup = (e: KeyboardEvent) => {
      if(document.activeElement && this.isTextBox(document.activeElement)) {
        return;
      }
      let keyCode = <KeyCode>e.keyCode;
      this.keysHeld.delete(keyCode);
      this.onKeyUp(e, e.keyCode);
      // e.preventDefault();
    }
  }

  onKeyDown(e: KeyboardEvent, keyCode: KeyCode, firstFrame: boolean) {
  }

  onKeyUp(e: KeyboardEvent, keyCode: KeyCode) {

  }

  isTextBox(element: Element) {
    if(!element) return false;
    var tagName = element.tagName.toLowerCase();
    if (tagName === 'textarea') return true;
    if (tagName !== 'input') return false;
    var type = element.getAttribute('type')?.toLowerCase();
    if (type === undefined) return false;
    // if any of these input types is not supported by a browser, it will behave as input type text.
    let inputTypes = ['text', 'password', 'number', 'email', 'tel', 'url', 'search', 'date', 'datetime', 'datetime-local', 'time', 'month', 'week']
    return inputTypes.indexOf(type) >= 0;
  }

}