import { SpriteEditor } from "./spriteEditor";
import { global } from "../Global";
import { NumberInput } from "../Components/NumberInput";
import { TextInput } from "../Components/TextInput";
import * as Helpers from "../helpers";

export function render(t: SpriteEditor) : JSX.Element {
  return (
    <>
      <div id="soj" style={{visibility:t.isLoading ? "visible" : "hidden"}}>
        <span id="soj-text">Loading...</span>
      </div>
      <div className="top-menu">
        <div>Version: {t?.config?.version} | F1: Hotkeys | Asset path: {t?.config?.assetPath}</div>
        <button onClick={() => t.reload()}>Reload Editor</button>&nbsp;
        {!t.customMapContext && <button onClick={() => t.onSwapFolderClick()}>{t.getSwapFolderButtonText()}</button>}
        {
          /*
          <span>Window Zoom Level: {t.windowZoom * 100}%</span>
          <button onClick={() => t.decreaseWindowZoom()}>Zoom Out</button>
          <button onClick={() => t.increaseWindowZoom()}>Zoom In</button>
          */
        }
      </div>
      <div id="sprite-editor">
        {renderSpriteListContainer(t)}
        {renderSpriteCanvas(t)}
        {renderSpritesheetCanvas(t)}
      </div>
    </>
  );
}

function renderSpriteListContainer(t: SpriteEditor): JSX.Element {

  let state = t.data;

  return (
    <div className="sprite-list-container">
      <h1>{t?.config?.isInSpriteModFolder ? "Sprite Mods" : "Game Sprites"}</h1>
      {
        !state.newSpriteActive &&
        <button onClick={() => t.newSprite()}>New Sprite</button>
      }
      {
        state.newSpriteActive &&
        <div id="newSpriteBox">
          <div id="newSpriteLabel">New Sprite</div>
          Name:
          <br/>
          <input type="text" onChange={e => { state.newSpriteName = e.target.value; t.changeState(); }} style={{width:"180px"}}/>
          <br/>
          Spritesheet:
          <br/>
          <select style={{width:"190px"}} value={state.newSpriteSpritesheetPath} onChange={e => {state.newSpriteSpritesheetPath = e.target.value; t.changeState(); }}>
            <option key={-1} value=""></option>
            {global.spritesheets.map((spritesheet, index) => (
              <option key={index} value={Helpers.fileName(spritesheet.path)}>{ Helpers.fileName(spritesheet.path) }</option>
            ))}
          </select>
          <br/>
          <button onClick={() => t.addSprite()}>Add</button>
        </div>
      }

      <div>Filter: <TextInput width="140px" initialValue={state.spriteFilter} onSubmit={str => { t.changeSpriteFilter(str); }} />
      </div>
      Filter mode: <select value={state.selectedFilterMode} onChange={e => { t.changeSpriteFilterMode(e.target.value); } }>
        <option value="contains">Contains</option>
        <option value="exactmatch">Exact match</option>
        <option value="startswith">Starts with</option>
        <option value="endswith">Ends with</option>
      </select>

      <div className="sprite-list-scroll">
        {
          t.getFilteredSprites().map((sprite, index) => (
          <div key={sprite.name} className={"sprite-item" + (sprite.name === state.selectedSprite?.name ? " selected" : "")} onClick={e => t.changeSprite(global.sprites.indexOf(sprite), false)}>
            { t.getSpriteDisplayName(sprite) }
          </div>
        ))}
      </div>
    </div>
  );
}

function renderSpriteCanvas(t: SpriteEditor) : JSX.Element {

  let state = t.data;

  return (
    <div className="canvas-section">
      <div className="canvas-wrapper" style={{width:700,height:600}} tabIndex={1}>
        <canvas id="canvas1" width="700" height="600"></canvas>
      </div>
      <div id="app2">
        {
          state.selectedSprite &&
          <div>

            {
            //<button onClick={e => t.forceAllDirty()}>Force all dirty</button>         
            //<button onClick={e => t.undo()}>Undo</button>
            //<button onClick={e => t.redo()}>Redo</button>
            }

            <button onClick={e => t.playAnim()}>{ state.isAnimPlaying ? "Stop" : "Play" }</button>
            <button onClick={e => t.saveSprite()} disabled={!state.isSelectedSpriteDirty()}>Save</button>
            <button onClick={e => t.saveSprites()} disabled={!t.isAnySpriteDirty()}>Save All</button>
            <button onClick={e => t.csssd()}>Force Dirty</button>
            {/*<button onClick={e => t.debug()}>Debug</button>*/}
      
            Zoom Level: <input style={{width:"50px"}} type="number" value={t.getZoom().toString()} onChange={e => t.setZoom(e.target.valueAsNumber)} />

            <input type="checkbox" checked={state.hideGizmos} onChange={e => {state.hideGizmos = e.target.checked; t.changeState();}}/>Hide gizmos
            <input type="checkbox" checked={state.moveChildren} onChange={e => {state.moveChildren = e.target.checked; t.changeState();}}/>Move children on frame move

            <div>
              Spritesheet:
              <select value={Helpers.fileName(state.selectedSpritesheetPath)} onChange={e => {state.setSelectedSpriteDirty(true); t.onSpritesheetChange(e.target.value);}}>
                {global.spritesheets.map((spritesheet, index) => (
                  <option key={index} value={Helpers.fileName(spritesheet.path)}>{ Helpers.fileName(spritesheet.path) }</option>
                ))}
              </select>
            </div>

            <div>
              Alignment:
              <select value={state.selectedSprite.alignment} onChange={e => {state.selectedSprite.alignment = e.target.value; t.csssd(); }}>
                {global.alignments.map((alignment, index) => (
                  <option key={index} value={alignment}>{ alignment }</option>
                ))}
              </select>
              Wrap mode:
              <select value={state.selectedSprite.wrapMode} onChange={e => {state.selectedSprite.wrapMode = e.target.value; t.csssd(); }}>
                {global.wrapModes.map((wrapMode, index) => (
                  <option key={index} value={wrapMode}>{ wrapMode }</option>
                ))}
              </select>
              Custom Align X:<NumberInput initialValue={state.selectedSprite.alignOffX || 0} onSubmit={(num: number) => {state.selectedSprite.alignOffX = num; t.csssd();}} />
              Custom Align Y:<NumberInput initialValue={state.selectedSprite.alignOffY || 0} onSubmit={(num: number) => {state.selectedSprite.alignOffY = num; t.csssd();}} />
            </div>

            <div className="hitbox-section">
              Global Hitboxes<br/>
              {state.selectedSprite.hitboxes?.map((hitbox, index) => (
                <div key={hitbox.selectableId} className={"frame-data" + (state.selection?.selectableId === hitbox.selectableId ? " selected-frame" : "")}>
                  w<NumberInput initialValue={hitbox.width} onSubmit={num => {hitbox.width = num; t.csssd();} } />
                  h<NumberInput initialValue={hitbox.height} onSubmit={num => {hitbox.height = num; t.csssd();} } />
                  x-off<NumberInput initialValue={hitbox.offset.x} onSubmit={num => {hitbox.offset.x = num; t.csssd();} } />
                  y-off<NumberInput initialValue={hitbox.offset.y} onSubmit={num => {hitbox.offset.y = num; t.csssd();} } />
                  flag<select value={hitbox.flag} onChange={e => {hitbox.flag = Number(e.target.value); t.csssd(); }}>
                    {global.hitboxFlags.map((flagName, index) => (
                      <option key={index} value={index}>{ flagName }</option>
                    ))}
                  </select>
                  name<TextInput width="50px" initialValue={hitbox.name ?? ""} onSubmit={str => {hitbox.name = str; t.csssd();}} />
                  trigger?<input type="checkbox" checked={hitbox.isTrigger} className="hitbox-tag-input" onChange={e => {hitbox.isTrigger = e.target.checked; t.csssd();} } />
                  <button onClick={e => t.selectHitbox(hitbox)}>Select</button>
                  <button onClick={e => t.deleteHitbox(state.selectedSprite.hitboxes, hitbox)}>Delete</button>
                </div>
              ))}
              <button onClick={e => t.addHitboxToSprite(false)}>New hitbox</button>
              {t.addRectMode !== 1 ?
                <button onClick={e => t.addHitboxToSprite(true)}>New hitbox (place coords)</button> :
                <span>Click canvas twice (top left point then bottom right point)</span>
              }
            </div>
            
            {state.selectedFrame &&
              <div className="hitbox-section">
                Frame Hitboxes<br/>
                {state.selectedFrame.hitboxes?.map((hitbox, index) => (
                  <div key={hitbox.selectableId} className={ 'frame-data' + (state.selection?.selectableId === hitbox.selectableId ? ' selected-frame': '') }>
                    w<NumberInput initialValue={hitbox.width} onSubmit={num => {hitbox.width = num; t.csssd();} } />
                    h<NumberInput initialValue={hitbox.height} onSubmit={num => {hitbox.height = num; t.csssd();} } />
                    x<NumberInput initialValue={hitbox.offset.x} onSubmit={num => {hitbox.offset.x = num; t.csssd();} } />
                    y<NumberInput initialValue={hitbox.offset.y} onSubmit={num => {hitbox.offset.y = num; t.csssd();} } />
                    flag<select value={hitbox.flag} onChange={e => {hitbox.flag = Number(e.target.value); t.csssd(); }}>
                      {global.hitboxFlags.map((flagName, index) => (
                        <option key={index} value={index}>{ flagName }</option>
                      ))}
                    </select>
                    name<TextInput width="50px" initialValue={hitbox.name ?? ""} onSubmit={str => {hitbox.name = str; t.csssd();}} />
                    tr?<input title="Trigger?" type="checkbox" checked={hitbox.isTrigger} style={{width:"20px"}} className="hitbox-tag-input" onChange={e => {hitbox.isTrigger = e.target.checked; t.csssd();} } />
                    <button onClick={e => t.selectHitbox(hitbox)}>Select</button>
                    <button onClick={e => t.deleteHitbox(state.selectedFrame.hitboxes, hitbox)}>Delete</button>
                    <button title="Add to Next Frame" onClick={e => {state.setSelectedSpriteDirty(true); t.applyHitboxToNextFrame(hitbox)}}>+ to next</button>
                  </div>
                ))}
                <button onClick={e => t.addHitboxToFrame(false)}>New hitbox</button>
                {t.addRectMode !== 2 ?
                  <button onClick={e => t.addHitboxToFrame(true)}>New hitbox (place coords)</button> :
                  <span>Click canvas twice (top left point then bottom right point)</span>
                }
              </div>
            }

            {state.selectedFrame &&
              <div className="hitbox-section">
                Frame POIs<br/>
                {state.selectedFrame.POIs?.map((poi, index) => (
                  <div key={poi.selectableId} className={ 'frame-data' + (state.selection?.selectableId === poi.selectableId ? ' selected-frame': '') }>
                    x<NumberInput initialValue={poi.x} onSubmit={num => {poi.x = num; t.csssd();}} />
                    y<NumberInput initialValue={poi.y} onSubmit={num => {poi.y = num; t.csssd();}} />
                    <TextInput width="50px" initialValue={poi.tags} onSubmit={str => {poi.tags = str; t.csssd();}} />
                    <button onClick={e => {state.setSelectedSpriteDirty(true); t.selectPOI(poi)}}>Select</button>
                    <button onClick={e => {state.setSelectedSpriteDirty(true); t.deletePOI(poi)}}>Delete</button>
                    <button onClick={e => {state.setSelectedSpriteDirty(true); t.applyPOIToAllFrames(poi)}}>Apply To All Frames</button>
                    <button onClick={e => {state.setSelectedSpriteDirty(true); t.movePOI(state.selectedFrame, index, -1)}}>↑</button>
                    <button onClick={e => {state.setSelectedSpriteDirty(true); t.movePOI(state.selectedFrame, index, 1)}}>↓</button>
                    <input type="checkbox" checked={poi.isHidden} onChange={e => {poi.isHidden = e.target.checked; t.changeState();} } />Hide
                  </div>
                ))}
                {t.addPOIMode ? 
                  <span>Click canvas to place POI</span> :
                  <button onClick={e => {state.setSelectedSpriteDirty(true); t.changeAddPOIMode(true)} }>New POI</button>
                }
              </div>
            }
          </div>
        }
      </div>
    </div>
  );
}

function renderSpritesheetCanvas(t: SpriteEditor) : JSX.Element {

  let state = t.data;

  return (
    <div className="canvas-section">
      <div className="canvas-wrapper" style={{width:"1000px",height:"700px"}} tabIndex={2}>
        <canvas id="canvas2" width="500" height="700"></canvas>
      </div>
      <div id="app3">
        {state.selectedSprite && t.selectedSpritesheet &&
          <div>
            {state.selectedSprite.frames.length > 0 &&
              <>
                <input type="checkbox" checked={state.moveToTopOrBottom} onChange={e => {state.moveToTopOrBottom = e.target.checked; t.changeState();}}/>Frame move/copy to top/bottom<br/>
              </>
            }
            {state.selectedSprite.frames.map((frame, index) => (
              <div key={frame.frameId} className={"frame-data unselectable" + (state.selectedFrameIndex === index ? " selected-frame" : "")} onClick={e => t.selectFrame(index)}>
                <div>
                  <b>Frame { index } </b>
                  duration: <NumberInput initialValue={frame.getDurationInFrames()} onSubmit={num => {frame.setDurationFromFrames(num); t.csssd();}}/>&nbsp;
                  x-off: <NumberInput initialValue={frame.offset.x} onSubmit={num => {frame.offset.x = num; t.csssd();}} />&nbsp;
                  y-off: <NumberInput initialValue={frame.offset.y} onSubmit={num => {frame.offset.y = num; t.csssd();}} />&nbsp;
                  <button title="Move Up" onClick={e => {e.stopPropagation(); t.moveFrame(index, -1);}}><img src="file:///images/moveup.png" /></button>
                  <button title="Move Down" onClick={e => {e.stopPropagation(); t.moveFrame(index, 1);}}><img src="file:///images/movedown.png" /></button>
                  <button title="Copy Up" onClick={e => {e.stopPropagation(); t.copyFrame(index, -1);}}><img src="file:///images/copyup.png" /></button>
                  <button title="Copy Down" onClick={e => {e.stopPropagation(); t.copyFrame(index, 1);}}><img src="file:///images/copydown.png" /></button>
                  <button title="Replace frame with current selection" onClick={e => {e.stopPropagation(); t.addPendingFrame(index);}}><img src="file:///images/overwrite.png" /></button>
                  <button title="Delete" onClick={e => {e.stopPropagation(); t.deleteFrame(index);}}><img src="file:///images/delete.png" /></button>
                  &nbsp;frame rect:&nbsp;
                  <NumberInput initialValue={frame.rect.topLeftPoint.x} onSubmit={num => {frame.rect.topLeftPoint.x = num; t.csssd(); }} />
                  <NumberInput initialValue={frame.rect.topLeftPoint.y} onSubmit={num => {frame.rect.topLeftPoint.y = num; t.csssd(); }} />
                  <NumberInput initialValue={frame.rect.botRightPoint.x} onSubmit={num => {frame.rect.botRightPoint.x = num; t.csssd(); }} />
                  <NumberInput initialValue={frame.rect.botRightPoint.y} onSubmit={num => {frame.rect.botRightPoint.y = num; t.csssd(); }} />
                  <span>{frame.rect.botRightPoint.x - frame.rect.topLeftPoint.x}x{frame.rect.botRightPoint.y - frame.rect.topLeftPoint.y}</span>
                  <button onClick={e => t.recomputeFrame(frame)}>Recompute</button>
                </div>
              </div>
            ))}
            {state.selectedSprite.frames.length > 0 &&
              <div>
                Set bulk duration:&nbsp; 
                <NumberInput initialValue={state.bulkDuration} onSubmit={num => {t.onBulkDurationChange(num);}} />
                <br/>
                Loop start frame:&nbsp;
                <NumberInput initialValue={state.selectedSprite.loopStartFrame} onSubmit={num => {state.selectedSprite.loopStartFrame = num; t.csssd();}} />
                <br/>
              </div>
            }
            {(state.selectedFrame || state.pendingFrame) ?
              <button onClick={() => t.addPendingFrame()} title="Add current selection as frame">Add as frame</button> :
              <i>Click on a clump of pixels to add as frame</i>
            }
            {state.selectedSprite.frames.length > 0 &&
              <span>
                <button onClick={() => t.reverseFrames()}>Reverse frames</button>    
                <button onClick={e => t.recomputeAllFrames()}>Recompute All</button>
              </span>
            }
          </div>
        }
      </div>
    </div>
  );
}
