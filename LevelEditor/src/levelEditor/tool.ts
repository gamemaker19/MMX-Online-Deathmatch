import { LevelEditor } from "./levelEditor";
import * as Helpers from "../helpers";
import * as DrawWrappers from "../drawWrappers";
import { KeyCode } from "../canvasUI";
import * as _ from "lodash";
import { Obj } from "../models/Obj";
import { Point } from "../models/Point";
import { Instance } from "../models/Instance";
import { addNeighbor, NavMeshNode } from "../models/NavMeshNode";
import { global } from "../Global";
import { Rect } from "../models/Rect";
import { Shape } from "../models/Shape";
import { getPixelClumpRect } from "../pixelClump";
import { Line } from "../models/Line";

export class Tool {

  levelEditor: LevelEditor;
  cursor: string = "";

  get mouseX() { return this.levelEditor.levelCanvas.mouseX; }
  get mouseY() { return this.levelEditor.levelCanvas.mouseY; }
  get ctx() { return this.levelEditor.levelCanvas.ctx; }
  get keysHeld() { return this.levelEditor.levelCanvas.keysHeld; }

	constructor(levelEditor: LevelEditor) {
		this.cursor = "default";
    this.levelEditor = levelEditor;
	}

	draw() {
    let data = this.levelEditor.data;
		//Draw individual borders
		data.selectedInstances.forEach((selection) => {
			DrawWrappers.drawRect(this.ctx, selection.getRect(), "", "green", 1, 0.75);
		});
	}

	onMouseMove(deltaX: number, deltaY: number) { }
	onMouseUp() { }
	onMouseLeaveCanvas() {}
	onMouseDown() { }
	onRightMouseDown() { }
	onKeyDown(keyCode: KeyCode, oneFrame: boolean) { }
	onKeyUp(keyCode: KeyCode) { }

	get data() {
		return this.levelEditor.data;
	}

	getObjsOver() {
		let hits = [];
    let data = this.levelEditor.data;
		for(let i = data.selectedLevel.instances.length - 1; i >= 0; i--) {
      var instance = data.selectedLevel.instances[i];
			if (instance.hidden) continue;
      if (!instance.isShape && instance.getRect().inRect(this.mouseX, this.mouseY)) {
				hits.push(instance);
			}
			else if (instance.isShape && Shape.inShape(this.mouseX, this.mouseY, instance.points)) {
				hits.push(instance);
			}
		}
		return hits;
  }
  
	drawVertex(x: number, y: number, icon: string) {
		DrawWrappers.drawCircle(this.ctx, x, y, 7 / this.levelEditor.levelCanvas.zoom, "white", "black", 1 / this.levelEditor.levelCanvas.zoom);
		DrawWrappers.drawText(this.ctx, icon, x, y, "black", "black", 14 / this.levelEditor.levelCanvas.zoom);
	}

  drawVertices() {
    let data = this.levelEditor.data;
    var points = data.selectedInstances[0].points;
    
		let spacing = 10;
		let fontSize = 14 / this.levelEditor.levelCanvas.zoom;
    //Top left point
    this.drawVertex(points[0].x - spacing, points[0].y, "↔");
    this.drawVertex(points[0].x, points[0].y - spacing, "↕");

    //Top right point
    this.drawVertex(points[1].x + spacing, points[1].y, "↔");
    this.drawVertex(points[1].x, points[1].y - spacing, "↕");

    //Bot right point
    this.drawVertex(points[2].x + spacing, points[2].y, "↔");
    this.drawVertex(points[2].x, points[2].y + spacing, "↕");

    //Bot left point
		if (points.length >= 4) {
			this.drawVertex(points[3].x - spacing, points[3].y, "↔");
			this.drawVertex(points[3].x, points[3].y + spacing, "↕");
		}

		DrawWrappers.drawText(this.ctx, "1", points[0].x, points[0].y, "white", "black", fontSize);
		DrawWrappers.drawText(this.ctx, "2", points[1].x, points[1].y, "white", "black", fontSize);
		DrawWrappers.drawText(this.ctx, "3", points[2].x, points[2].y, "white", "black", fontSize);
		if (points.length >= 4) {
			DrawWrappers.drawText(this.ctx, "4", points[3].x, points[3].y, "white", "black", fontSize);
		}
  }

	snapOthersToSelection() {
		let leState = this.levelEditor.data;
		let si = this.levelEditor.data.selectedInstances[0];
		if (si && leState.snapCollision && si.objectName === "Collision Shape") {
			for (let instance of leState.selectedLevel.instances) {
				if (leState.selectedInstanceIds.includes(instance.id)) continue;
				if (instance.objectName !== "Collision Shape") continue;
				let shape1 = new Shape(si.points);
				let rect = shape1.getBoundingRect();
				rect.topLeftPoint.x -= 6;
				rect.topLeftPoint.y -= 6;
				rect.botRightPoint.x += 6;
				rect.botRightPoint.y += 6;
				let shape2 = new Shape(instance.points);
				let rect2 = shape2.getBoundingRect();
				if (!rect.overlaps(rect2)) continue;
				instance.snapCollisionShape(leState.selectedLevel.instances);
			}
		}
	}

	snapSelectionToOthers() {
		let leState = this.levelEditor.data;
		let si = this.levelEditor.data.selectedInstances[0];
		if (leState.snapCollision && si.objectName === "Collision Shape") {
			si.snapCollisionShape(leState.selectedLevel.instances);
		}
	}
}

export class CreateInstanceTool extends Tool {

	constructor(levelEditor: LevelEditor) {
		super(levelEditor);
		this.cursor = "crosshair";
	}

	onMouseDown() {
    let data = this.levelEditor.data;
		let newName = data.selectedLevel.getNewInstanceName(data.selectedObject);
		var instance = Instance.CreateObjectInstance(newName, undefined, data.selectedObject.name, new Point(this.mouseX, this.mouseY));
    data.selectedLevel.instances.push(instance);
    data.selectedObjectIndex = -1;
    data.selectedInstanceIds = [instance.id];
		this.levelEditor.switchTool(new SelectTool(this.levelEditor));
		data.setSelectedLevelDirty(true);
		this.levelEditor.changeState();
	}

	onKeyDown(keyCode: KeyCode, oneFrame: boolean) {
    let data = this.levelEditor.data;
		if(keyCode === KeyCode.ESCAPE) {
      data.selectedObjectIndex = -1;
			this.levelEditor.switchTool(new SelectTool(this.levelEditor));
			this.levelEditor.changeState();
		}
	}

}

export class CreateTool extends Tool {

  obj: Obj;
	constructor(levelEditor: LevelEditor, obj: Obj) {
    super(levelEditor);
    this.obj = obj;
		this.cursor = "crosshair";
	}

	onMouseDown() {
    let data = this.levelEditor.data;
    var v1 = new Point(this.mouseX, this.mouseY);
    var v2 = new Point(this.mouseX + 4, this.mouseY);
    var v3 = new Point(this.mouseX + 4, this.mouseY + 4);
    var v4 = new Point(this.mouseX, this.mouseY + 4);
    
		let newName = data.selectedLevel.getNewInstanceName(this.obj);
    var collisionBox = Instance.CreateShapeInstance(newName, undefined, this.obj.name, [v1, v2, v3, v4]);
    
		data.selectedLevel.addInstance(collisionBox);
    data.selectedInstanceIds.push(collisionBox.id);
    data.selectedObjectIndex = -1;
    this.levelEditor.switchTool(new ResizeTool(this.levelEditor, "se-resize"));
		data.setSelectedLevelDirty(true);
		this.levelEditor.forceUpdate();
	}

	onRightMouseDown() {
    if (this.levelEditor.data.selectedObject.name !== "Backwall Zone") return;
		if (this.levelEditor.config.isProd) return;		
    if (this.levelEditor.selectedBackground2 === null) return;

		if (!this.levelEditor.selectedBackground2.imgArr) {
			this.levelEditor.selectedBackground2.lazyLoadImgArr();
		}
    let rect = getPixelClumpRect(this.mouseX, this.mouseY, this.levelEditor.selectedBackground2.imgArr);
    if (rect) {
			let data = this.levelEditor.data;
			let newInstanceNum = data.selectedLevel.getNewInstanceNum(this.obj);
			var collisionBox = Instance.CreateShapeInstance(this.obj.name + newInstanceNum, undefined, this.obj.name, rect.getPoints());
			
			data.selectedLevel.addInstance(collisionBox);
			data.setSelectedLevelDirty(true);
			this.levelEditor.changeState();
    }
	}

	onKeyDown(keyCode: KeyCode, oneFrame: boolean) {
		if (keyCode === KeyCode.ESCAPE) {
      this.data.selectedObjectIndex = -1;
			this.levelEditor.switchTool(new SelectTool(this.levelEditor));
			this.levelEditor.changeState();
		}
	}

}

export class ResizeTool extends Tool {
  resizeDir: string;
  init_x: number;
  init_y: number;
	constructor(levelEditor: LevelEditor, resizeDir: string) {
		super(levelEditor);
		this.resizeDir = resizeDir;
		this.cursor = resizeDir;
		this.init_x = this.mouseX;
		this.init_y = this.mouseY;
		//Clear out saved percentages

		levelEditor.data.selectedInstances.forEach((selection) => {
			selection.clearPointPercents();
		});
	}

	onMouseUp() {
    //normalize points
    for(let i = 0; i < this.data.selectedInstances.length; i++) {
      this.data.selectedInstances[i].normalizePoints();
    }
		this.snapOthersToSelection();
		this.levelEditor.switchTool(new SelectTool(this.levelEditor));
		this.data.setSelectedLevelDirty(true);
		this.levelEditor.changeState();
	}

	onMouseMove(deltaX: number, deltaY: number) {
		for (let i = 0; i < this.data.selectedInstances.length; i++) {
			this.data.selectedInstances[i].resize(deltaX, deltaY, this.resizeDir);
		}
		this.levelEditor.redraw();
	}

	onMouseLeaveCanvas() {
		this.levelEditor.switchTool(new SelectTool(this.levelEditor));
		this.data.selectedLevel.isDirty = true;
		this.levelEditor.changeState();
	}

}

export class MoveTool extends Tool {

	moved: boolean;
	constructor(levelEditor: LevelEditor) {
		super(levelEditor);
		this.cursor = "default";
	}

	onMouseMove(deltaX: number, deltaY: number) {
    this.data.selectedInstances.forEach(function(selection) {
			selection.move(deltaX, deltaY);
		});
		this.moved = true;
		this.levelEditor.redraw();
	}

	onMouseUp() {
    this.data.selectedInstances.forEach(function(selection) {
			selection.normalizePoints();
		});
		this.levelEditor.switchTool(new SelectTool(this.levelEditor));
		if (this.moved) {
			this.snapSelectionToOthers();
			this.data.selectedLevel.isDirty = true;
		}
		this.levelEditor.changeState();
	}

}

export class DragSelectTool extends Tool {

	constructor(levelEditor: LevelEditor) {
		super(levelEditor);
		this.cursor = "default";
	}

	onMouseMove(deltaX: number, deltaY: number) {
		this.levelEditor.redraw();
	}

	onMouseUp() {
		let lc = this.levelEditor.levelCanvas;
		let rect = new Rect(lc.dragLeftX, lc.dragTopY, lc.dragRightX, lc.dragBotY);
		let selectionIds = [];
		for (let instance of this.levelEditor.data.selectedLevel.instances) {
			if (!instance.hidden && rect.overlaps(instance.getRect())) {
				selectionIds.push(instance.id);
			}
		}
		this.levelEditor.data.selectedInstanceIds = selectionIds;
		this.levelEditor.changeState();
		this.levelEditor.switchTool(new SelectTool(this.levelEditor));
	}

	onMouseLeaveCanvas(): void {
		this.onMouseUp();
	}

	draw() {
		let lc = this.levelEditor.levelCanvas;
		DrawWrappers.drawRect(this.ctx, new Rect(lc.dragLeftX, lc.dragTopY, lc.dragRightX, lc.dragBotY), "", "blue", 1);
	}

}

export class SelectTool extends Tool {

	constructor(levelEditor: LevelEditor) {
		super(levelEditor);
	}

	onKeyDown(keyCode: KeyCode, oneFrame: boolean) {
    let data = this.levelEditor.data;
    if (keyCode === KeyCode.TAB && data.selectedInstances.length === 1 && data.selectedInstances[0].isShape) {
      this.levelEditor.switchTool(new SelectVertexTool(this.levelEditor));
			this.levelEditor.redraw();
      return;
    }
		
		data.selectedInstances.forEach((selection) => {
			selection.clearPointPercents();
		});

		let stateChanged = false;
		let dirtyChanged = false;

		if (data.selectedInstances.length === 2 && data.selectedInstances[0].objectName === "Node" && data.selectedInstances[1].objectName === "Node") {
			var node1 = _.find(data.selectedLevel.instances, i => i.id === data.selectedInstanceIds[0]);
			var node2 = _.find(data.selectedLevel.instances, i => i.id === data.selectedInstanceIds[1]);
			let navMeshNode1 = (node1.properties || {}) as NavMeshNode;
			navMeshNode1.neighbors = navMeshNode1.neighbors || [];
			let navMeshNode2 = (node2.properties || {}) as NavMeshNode;
			navMeshNode2.neighbors = navMeshNode2.neighbors || [];

			let connectKey = KeyCode.F;
			let oneWayConnectKey = KeyCode.V; 
			let unconnectKey = KeyCode.X;
			let insertMiddleKey = KeyCode.R;
			if (keyCode === connectKey || keyCode === oneWayConnectKey) {
				
				addNeighbor(navMeshNode1, node2.name);
				node1.onUpdatePropertiesJson();

				if (keyCode === connectKey) {
					addNeighbor(navMeshNode2, node1.name);
					node2.onUpdatePropertiesJson();
				}

				stateChanged = true;
				dirtyChanged = true;
			}
			else if (keyCode === unconnectKey || keyCode === insertMiddleKey) {
	
				_.remove(navMeshNode1.neighbors, n => n.nodeName === node2.name);
				_.remove(navMeshNode2.neighbors, n => n.nodeName === node1.name);
	
				if (keyCode === insertMiddleKey) {
					let nodeObj = global.getObjectByName("Node");
					let newInstanceNum = data.selectedLevel.getNewInstanceNum(nodeObj);
					let halfwayPos = new Point((node1.pos.x + node2.pos.x) / 2, (node1.pos.y + node2.pos.y) / 2);
					var newNode = Instance.CreateObjectInstance(nodeObj.name + newInstanceNum, undefined, nodeObj.name, halfwayPos);
					data.selectedLevel.instances.push(newNode);
					
					addNeighbor(navMeshNode1, newNode.name);
					addNeighbor(navMeshNode2, newNode.name);

					let newNavMeshNode = (newNode.properties || {}) as NavMeshNode;
					newNavMeshNode.neighbors = newNavMeshNode.neighbors || [];
					addNeighbor(newNavMeshNode, node1.name);
					addNeighbor(newNavMeshNode, node2.name);

					newNode.onUpdatePropertiesJson();
				}

				node1.onUpdatePropertiesJson();
				node2.onUpdatePropertiesJson();
	
				stateChanged = true;
				dirtyChanged = true;
			}
		}

		if (keyCode === KeyCode.ESCAPE) {
			data.selectedInstanceIds = [];
			stateChanged = true;
    }
    
    if (keyCode === KeyCode.DELETE) {
      for(var i = data.selectedInstances.length - 1; i >= 0; i--) {
        var instanceToDelete = data.selectedInstances[i];
        _.pull(data.selectedLevel.instances, instanceToDelete);
        _.pull(data.selectedInstanceIds, instanceToDelete.id);

				// Remove node links to neighbors of deleted node to this node
				let instanceToDeleteNode = instanceToDelete.properties as NavMeshNode;
				for (let neighbor of instanceToDeleteNode?.neighbors ?? []) {
					let neighborOfNodeToDelete = _.find(data.selectedLevel.instances, i => i.name === neighbor.nodeName);
					let neighborNavMeshNode = neighborOfNodeToDelete?.properties as NavMeshNode;
					if (neighborNavMeshNode?.neighbors) {
						_.remove(neighborNavMeshNode.neighbors, n => n.nodeName === instanceToDelete.name);
						neighborOfNodeToDelete.onUpdatePropertiesJson();
					}
				}

				stateChanged = true;
				dirtyChanged = true;
      }
    }

		// SHAPE SECTION MOVE/RESIZE
		if (data.selectedInstances.length === 1 && data.selectedInstances[0].isShape) {
			if (!this.keysHeld.has(KeyCode.SHIFT)) {
				if (keyCode === KeyCode.A) {
					for(var selection of data.selectedInstances) {
						selection.resize(-1, 0, "w-resize");
						stateChanged = true;
						dirtyChanged = true;
					}
				}
				else if (keyCode === KeyCode.D) {
					for(var selection of data.selectedInstances) {
						selection.resize(1, 0, "w-resize");
						stateChanged = true;
						dirtyChanged = true;
					}
				}
				else if (keyCode === KeyCode.W) {
					for(var selection of data.selectedInstances) {
						selection.resize(0, -1, "n-resize");
						stateChanged = true;
						dirtyChanged = true;
					}
				}
				else if (keyCode === KeyCode.S) {
					for(var selection of data.selectedInstances) {
						selection.resize(0, 1, "n-resize");
						stateChanged = true;
						dirtyChanged = true;
					}
				}
			}
			else {
				//resizeDir can be: ["nw-resize", "n-resize", "ne-resize", "e-resize", "se-resize", "s-resize", "sw-resize", "w-resize"];
				if (keyCode === KeyCode.A) {
					for(var selection of data.selectedInstances) {
						selection.resize(-1, 0, "e-resize");
						stateChanged = true;
						dirtyChanged = true;
					}
				}
				else if (keyCode === KeyCode.D) {
					for(var selection of data.selectedInstances) {
						selection.resize(1, 0, "e-resize");
						stateChanged = true;
						dirtyChanged = true;
					}
				}
				else if (keyCode === KeyCode.W) {
					for(var selection of data.selectedInstances) {
						selection.resize(0, -1, "s-resize");
						stateChanged = true;
						dirtyChanged = true;
					}
				}
				else if (keyCode === KeyCode.S) {
					for(var selection of data.selectedInstances) {
						selection.resize(0, 1, "s-resize");
						stateChanged = true;
						dirtyChanged = true;
					}
				}
			}
			if (keyCode === KeyCode.LEFT) {
				for(var selection of data.selectedInstances) {
					selection.move(-1, 0);
					stateChanged = true;
					dirtyChanged = true;
				}
			}
			else if(keyCode === KeyCode.RIGHT) {
				for(var selection of data.selectedInstances) {
					selection.move(1, 0);
					stateChanged = true;
					dirtyChanged = true;
				}
			}
			else if(keyCode === KeyCode.UP) {
				for(var selection of data.selectedInstances) {
					selection.move(0, -1);
					stateChanged = true;
					dirtyChanged = true;
				}
			}
			else if(keyCode === KeyCode.DOWN) {
				for(var selection of data.selectedInstances) {
					selection.move(0, 1);
					stateChanged = true;
					dirtyChanged = true;
				}
			}
		}
		// INSTANCE SECTION MOVE/RESIZE
		else if (data.selectedInstances.length >= 1) {
			if (keyCode === KeyCode.A) {
				for(var selection of data.selectedInstances) {
					selection.move(-1, 0);
					stateChanged = true;
					dirtyChanged = true;
				}
			}
			else if(keyCode === KeyCode.D) {
				for(var selection of data.selectedInstances) {
					selection.move(1, 0);
					stateChanged = true;
					dirtyChanged = true;
				}
			}
			else if(keyCode === KeyCode.W) {
				for(var selection of data.selectedInstances) {
					selection.move(0, -1);
					stateChanged = true;
					dirtyChanged = true;
				}
			}
			else if(keyCode === KeyCode.S) {
				for(var selection of data.selectedInstances) {
					selection.move(0, 1);
					stateChanged = true;
					dirtyChanged = true;
				}
			}
		}

		if (dirtyChanged) {
			this.snapOthersToSelection();
			data.setSelectedLevelDirty(true);
		}
		if (stateChanged) {
			this.levelEditor.changeState();		
		}
	}

	onMouseMove(deltaX: number, deltaY: number) {
    let canvas1 = this.levelEditor.levelCanvas.canvas;
		if (this.data.selectedInstances.length > 0 && _.some(this.data.selectedInstances, instance => instance.isShape)) {

			this.calcMouseOverES();

      let lo = this.lo;
      let to = this.to;
      let bo = this.bo;
      let ro = this.ro;

			let resizeIcons = ["nw-resize", "n-resize", "ne-resize", "e-resize", "se-resize", "s-resize", "sw-resize", "w-resize"];
      let range07 = function(val: number) { while(val > 7) { val -= 8; } return val; }
      
			if (lo && to) {
				canvas1.style.cursor = resizeIcons[range07(0)];
			}
			else if (lo && bo) {
				canvas1.style.cursor = resizeIcons[range07(6)];
			}
			else if (ro && to) {
				canvas1.style.cursor = resizeIcons[range07(2)];
			}
			else if (ro && bo) {
				canvas1.style.cursor = resizeIcons[range07(4)];
			}
			else if (lo) {
				canvas1.style.cursor = resizeIcons[range07(7)];
			}
			else if (ro) {
				canvas1.style.cursor = resizeIcons[range07(3)];	
			}
			else if (to) {
				canvas1.style.cursor = resizeIcons[range07(1)];
			}
			else if (bo) {
				canvas1.style.cursor = resizeIcons[range07(5)];
			}
			else {
				canvas1.style.cursor = "default";
      }
		}
		this.levelEditor.redraw();
	}

  lo: boolean;
  to: boolean;
  bo: boolean;
  ro: boolean;
	onMouseDown() {
    let data = this.levelEditor.data;
    if (data.selectedInstances.length > 0 && _.some(this.data.selectedInstances, instance => instance.isShape)) {
      this.calcMouseOverES();
      
      var lo = this.lo;
      var to = this.to;
      var bo = this.bo;
      var ro = this.ro;

			if(lo && to) {
				this.levelEditor.switchTool(new ResizeTool(this.levelEditor, "nw-resize"));
				return;
			}
			else if(lo && bo) {
				this.levelEditor.switchTool(new ResizeTool(this.levelEditor, "sw-resize"));
				return;
			}
			else if(ro && to) {
				this.levelEditor.switchTool(new ResizeTool(this.levelEditor, "ne-resize"));
				return;
			}
			else if(ro && bo) {
				this.levelEditor.switchTool(new ResizeTool(this.levelEditor, "se-resize"));
				return;
			}
			else if(lo) {
				this.levelEditor.switchTool(new ResizeTool(this.levelEditor, "w-resize"));
				return;
			}
			else if(ro) {
				this.levelEditor.switchTool(new ResizeTool(this.levelEditor, "e-resize"));	
				return;
			}
			else if(to) {
				this.levelEditor.switchTool(new ResizeTool(this.levelEditor, "n-resize"));
				return;
			}
			else if(bo) {
				this.levelEditor.switchTool(new ResizeTool(this.levelEditor, "s-resize"));
				return;
			}
		}

    /*
		if(key_down('ctrl')) {
			switchTool(new DragSelectTool(mouse_x, mouse_y));
			return;
    }
    */

		var clickedObjs = this.getObjsOver();
		if (clickedObjs.length === 1 || (clickedObjs.length > 1 && this.keysHeld.has(KeyCode.SHIFT))) {
			let clickedObj = clickedObjs[0];
			//If not already selected and shift not held, replace current selection
			if (!data.selectedInstanceIds.includes(clickedObj.id) && !this.keysHeld.has(KeyCode.SHIFT)) {
				data.selectedInstanceIds = [];
			}
			if (!data.selectedInstanceIds.includes(clickedObj.id)) {
        data.selectedInstanceIds.push(clickedObj.id);
      }
			this.levelEditor.changeState();
			this.levelEditor.switchTool(new MoveTool(this.levelEditor));
		}
		else if (clickedObjs.length > 1) {
			if (this.keysHeld.has(KeyCode.CONTROL) && data.selectedInstanceIds.length === 1) {
				let selectedInstanceId = data.selectedInstanceIds[0];
				let index = clickedObjs.findIndex(o => o.id === selectedInstanceId);
				if (index !== -1) {
					let instanceBelow = clickedObjs[index + 1];
					if (!instanceBelow) instanceBelow = clickedObjs[0];
					data.selectedInstanceIds = [instanceBelow.id];
				}
				else {
					data.selectedInstanceIds = [clickedObjs[0].id];
				}
			}
			else {
				if (data.selectedInstanceIds.length <= 1) {
					data.selectedInstanceIds = [clickedObjs[0].id];
				}
				else {
					if (!data.selectedInstanceIds.includes(clickedObjs[0].id)) {
						data.selectedInstanceIds.push(clickedObjs[0].id);
					}
				}
			}
			this.levelEditor.changeState();
			this.levelEditor.switchTool(new MoveTool(this.levelEditor));
		}
		else if (data.selectedInstances.length > 0 && data.selectedInstances[0].getRect().inRect(this.mouseX, this.mouseY)) {
			this.levelEditor.switchTool(new MoveTool(this.levelEditor));
		}
		else {
			if (!this.keysHeld.has(KeyCode.SHIFT) && data.selectedInstanceIds.length > 0) {
				data.selectedInstanceIds = [];
				this.levelEditor.changeState();
      }
			this.levelEditor.switchTool(new DragSelectTool(this.levelEditor));
		}
	}

	calcMouseOverES() {
    let data = this.levelEditor.data;
		var vertex_box = data.selectedInstances[0].getRect().getPoints();

		const threshold = Helpers.clampMin(4 / this.levelEditor.getZoom(), 1);
		this.to = Line.inLine(this.mouseX, this.mouseY, vertex_box[0].x, vertex_box[0].y, vertex_box[1].x, vertex_box[1].y, threshold);
		this.ro = Line.inLine(this.mouseX, this.mouseY, vertex_box[1].x, vertex_box[1].y, vertex_box[2].x, vertex_box[2].y, threshold);
		this.bo = Line.inLine(this.mouseX, this.mouseY, vertex_box[2].x, vertex_box[2].y, vertex_box[3].x, vertex_box[3].y, threshold);
		this.lo = Line.inLine(this.mouseX, this.mouseY, vertex_box[3].x, vertex_box[3].y, vertex_box[0].x, vertex_box[0].y, threshold);

		/*
		//Sprites and objects selected: can't select top or left
		if(selections.some(function(elem) { return elem instanceof Sprite; })) {
			lo = false;
			to = false;
		}
		*/
	}

	onMouseUp() {
	}

	onMouseLeaveCanvas() {
	}

}

export class SelectVertexTool extends Tool {

	constructor(levelEditor: LevelEditor) {
		super(levelEditor);
  }
  
  draw() {
    this.drawVertices();
  }

	onKeyDown(keyCode: KeyCode, oneFrame: boolean) { 
		if(keyCode === KeyCode.TAB || keyCode === KeyCode.ESCAPE) {
			this.levelEditor.switchTool(new SelectTool(this.levelEditor));
			this.levelEditor.redraw();
    }
	}

	onMouseDown() {
    let data = this.levelEditor.data;
    var points = data.selectedInstances[0].points;
    
    //Top left point
    if (Helpers.inCircle(this.mouseX, this.mouseY, points[0].x - 10, points[0].y, 5)) this.levelEditor.switchTool(new MoveVertexTool(this.levelEditor, points[0], "horizontal"));
    if (Helpers.inCircle(this.mouseX, this.mouseY, points[0].x, points[0].y - 10, 5)) this.levelEditor.switchTool(new MoveVertexTool(this.levelEditor, points[0], "vertical"));

    //Top right point
    if (Helpers.inCircle(this.mouseX, this.mouseY, points[1].x + 10, points[1].y, 5)) this.levelEditor.switchTool(new MoveVertexTool(this.levelEditor, points[1], "horizontal"));
    if (Helpers.inCircle(this.mouseX, this.mouseY, points[1].x, points[1].y - 10, 5)) this.levelEditor.switchTool(new MoveVertexTool(this.levelEditor, points[1], "vertical"));

    //Bot right point
    if (Helpers.inCircle(this.mouseX, this.mouseY, points[2].x + 10, points[2].y, 5)) this.levelEditor.switchTool(new MoveVertexTool(this.levelEditor, points[2], "horizontal"));
    if (Helpers.inCircle(this.mouseX, this.mouseY, points[2].x, points[2].y + 10, 5)) this.levelEditor.switchTool(new MoveVertexTool(this.levelEditor, points[2], "vertical"));

    //Bot left point
		if (points.length >= 4) {
			if (Helpers.inCircle(this.mouseX, this.mouseY, points[3].x - 10, points[3].y, 5)) this.levelEditor.switchTool(new MoveVertexTool(this.levelEditor, points[3], "horizontal"));
			if (Helpers.inCircle(this.mouseX, this.mouseY, points[3].x, points[3].y + 10, 5)) this.levelEditor.switchTool(new MoveVertexTool(this.levelEditor, points[3], "vertical"));
		}

		this.levelEditor.redraw();
	}

	onMouseUp() {
	}

	onMouseLeaveCanvas() {
	}

}

export class MoveVertexTool extends Tool {

  dir: string = "";
  grabbedVertex: Point;
  //dir can be horizontal or vertical
	constructor(levelEditor: LevelEditor, grabbedVertex: Point, dir: string) {
		super(levelEditor);
		this.cursor = "default";
    this.grabbedVertex = grabbedVertex;
    this.dir = dir;
  }
  
  draw() {
    this.drawVertices();
  }

	onMouseMove(deltaX: number, deltaY: number) {	
    if (this.dir === "horizontal") this.grabbedVertex.x += deltaX;
    else if (this.dir === "vertical") this.grabbedVertex.y += deltaY;
    else throw "Direction not horizontal or vertical";
		this.levelEditor.redraw();
	}

	onMouseUp() {
		this.levelEditor.switchTool(new SelectVertexTool(this.levelEditor));
		this.snapOthersToSelection();
		this.levelEditor.cssld();
	}

}