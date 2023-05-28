import _ from "lodash";
import React from "react";
import { Config } from "./config";

const MAX_UNDOS: number = 100;

export class BaseEditor<TState> extends React.Component<{}, {}> {
  
  undoIndex: number = 0;
  history: TState[] = [];
  data: TState;
  config: Config;
  isLoading: boolean;
  windowZoom: number = 1;

  constructor(props: {}) {
    super(props);
    this.state = {};
    this.isLoading = true;
  }

  redraw(redrawBackgrounds: boolean = false) : void {}

  decreaseWindowZoom() {
    this.windowZoom-=0.2;
    if (this.windowZoom < 1) this.windowZoom = 1;
    window.Main.setZoomLevel(this.windowZoom);
    this.forceUpdate();
  }
  
  increaseWindowZoom() {
    this.windowZoom+=0.2;
    if (this.windowZoom > 2) this.windowZoom = 2;
    window.Main.setZoomLevel(this.windowZoom);
    this.forceUpdate();
  }

  changeState() : void {

    let lastState = this.history[this.undoIndex];
    if (lastState && _.isEqual(lastState, this.data)) {
      // console.log("States are equal, not updating");
      return;
    }

    let currentClonedData = this.data;
    this.data = _.cloneDeep(currentClonedData);

    this.history.splice(this.undoIndex + 1);
    this.history.push(currentClonedData);
    this.undoIndex = this.history.length - 1;
    if (this.history.length > MAX_UNDOS) {
      this.history.shift();
      this.undoIndex--;
    }

    try {
      this.redraw();
    } catch (error) {
      console.error("Redraw failed on state change. Error: ");
      console.error(error);
    }

    this.forceUpdate();
    
    console.log("Change state. Undo index: " + this.undoIndex + ", history length: " + this.history.length);
    //console.trace();
    //console.log(currentClonedData);
  }

  undo() {
    if (this.undoIndex <= 0) return;
    this.undoIndex--;
    let undoState = this.history[this.undoIndex];
    this.data = _.cloneDeep(undoState);
    this.redraw();
    this.forceUpdate();

    console.log("Undo. Undo index: " + this.undoIndex + ", history length: " + this.history.length);
    //console.log(this.data);
  }

  redo() {
    if (this.undoIndex >= this.history.length - 1) return;
    this.undoIndex++;
    let redoState = this.history[this.undoIndex];
    this.data = _.cloneDeep(redoState);
    this.forceUpdate();
    this.redraw();

    console.log("Redo. Redo index: " + this.undoIndex + ", history length: " + this.history.length);
    //console.log(this.data);
  }
}