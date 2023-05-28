import { LevelEditor } from "./levelEditor";
import { global } from "../Global";
import { NumberInput } from "../Components/NumberInput";
import * as Helpers from "../helpers";
import { Parallax } from "../models/Parallax";
import _ from "lodash";
import { MirrorEnabled } from "../enums";
import { NavMeshNeighbor } from "../models/NavMeshNode";
import { TextInput } from "../Components/TextInput";
import { PropertyInput } from "../Components/PropertyInput";

export function render(t: LevelEditor) : JSX.Element {
  return (
    <>
      <div id="soj" style={{visibility: t.isLoading ? "visible" : "hidden"}}>
        <span id="soj-text">Loading...</span>
      </div>
      <div className="top-menu">
        <div>Version: {t?.config?.version} | F1: Hotkeys | Asset path: {t?.config?.assetPath}</div>
        <button onClick={() => t.reload()}>Reload Editor</button>&nbsp;
        <button onClick={() => t.onSwapFolderClick()}>{t.getSwapFolderButtonText()}</button>&nbsp;
        {t?.config?.isInMapModFolder && <button disabled={!t.data.selectedLevel} onClick={e => t.setupMapSpriteEditor()}>Edit Map Sprites</button>}
        &nbsp;|&nbsp;
        <span>Console Command:</span>
        &nbsp;
        <TextInput width="140px" initialValue={t.consoleCommand} onSubmit={str => { t.consoleCommand = str; t.forceUpdate(); }} />
        &nbsp;
        <button onClick={() => t.runConsoleCommand()}>Run command</button>
        &nbsp;
        <i>{t.consoleCommandResult}</i>
      </div>
      <div id="level-editor">
        {renderLevelList(t)}
        {renderObjectList(t)}
        {renderLevelCanvas(t)}
        {renderInstanceList(t)}
      </div>
    </>
  );
}

function renderLevelList(t: LevelEditor): JSX.Element {
  let state = t.data;
  return (
    <div className="sprite-list-container">
      <h1>{t?.config?.isInMapModFolder ? "Custom Maps" : "Official Maps"}</h1>
      {
        !state.newLevelActive &&
        <button onClick={() => t.newLevel()}>New Map</button>
      }
      {
        state.newLevelActive &&
        <div id="newSpriteBox">
          <div id="newSpriteLabel">New Map</div>
          Name:
          <br/>
          <input type="text" maxLength={40} onChange={e => { state.newLevelName = e.target.value; t.changeState(); }} style={{width:"180px"}}/>
          <br/>
          <button onClick={() => t.addLevel()}>Add</button>
        </div>
      }
      <div>Filter: <TextInput width="140px" initialValue={state.levelFilter} onSubmit={str => { t.changeLevelFilter(str); }} />
      </div>
      Filter mode: <select value={state.selectedFilterMode} onChange={e => { t.changeLevelFilterMode(e.target.value); } }>
        <option value="contains">Contains</option>
        <option value="exactmatch">Exact match</option>
        <option value="startswith">Starts with</option>
        <option value="endswith">Ends with</option>
      </select>

      <div>
        <div className="sprite-list-scroll">
          {
            t.getFilteredLevels().map((level, index) => (
              <div key={level.name} className={ "sprite-item" + (level.name === state.selectedLevel?.name ? " selected" : "") } onClick={e => t.changeLevel(level)}>
                { t.getLevelDisplayName(level) }
              </div>
            ))
          }
        </div>
      </div>
    </div>
  );
}

function renderObjectList(t: LevelEditor): JSX.Element {
  let state = t.data;
  return (
    <div className="sprite-list-container">
      <h1>Objects</h1>
      <div className="sprite-list-scroll">
        {
          global.objects.map((obj, index) => {
            if (obj.exclusiveMap && obj.exclusiveMap !== state.selectedLevel?.name) {
              return null;
            }
            if (t.config && t.config.isProd && (obj.name === "Jape Memorial")) {
              return null;
            }
            return (
              <div key={obj.name} className={"sprite-item" + (obj.name === state.selectedObject?.name ? " selected" : "") } onClick={e => t.changeObject(obj)}>
                { obj.name }
              </div>
            );
          })
        }
        <br/>
      </div>
    </div>
  );
}

function renderLevelCanvas(t: LevelEditor): JSX.Element {
  let state = t.data;
  return (
  <div className="canvas-section">
    <div style={{display:"inline-block"}}>
      <div className="level-canvas-wrapper" style={{width:t.canvasWidth.toString() + "px", height: t.canvasHeight.toString() + "px"}} tabIndex={1}>
        <canvas id="level-canvas" width={t.canvasWidth.toString()} height={t.canvasHeight.toString()}></canvas>
      </div>
    </div>

    {
      state.selectedLevel &&

      <div style={{margin:"2px"}}>
        
        {!t.isOptimizedMode() &&
          <div>
            OPTIMIZED MODE OFF <button onClick={e => t.setOptimizedMode(true)}>Turn On</button>
          </div>
        }

        {t.isOptimizedMode() &&
          <table style={{border:"1px solid black"}}>
            <tbody>
              <tr>
                <td>
                  <div>OPTIMIZED MODE ON</div>
                  <button onClick={e => t.setOptimizedMode(false)}>Turn Off</button>
                </td>
                <td style={{paddingLeft:"185px",paddingRight:"15px"}}>
                  Scroll
                </td>
                <td>
                  <div>
                    <button onClick={e => t.fastScroll(0, -1)} style={{marginLeft:"30px"}}>↑</button>
                    <div>
                      <button onClick={e => t.fastScroll(-1, 0)}>←</button>
                      <button onClick={e => t.fastScroll(0, 1)}>↓</button>
                      <button onClick={e => t.fastScroll(1, 0)}>→</button>
                    </div>
                  </div>
                </td>
                <td style={{paddingLeft:"100px",paddingRight:"15px"}}>
                  Scroll Page
                </td>
                <td>
                  <div>
                    <button onClick={e => t.fastScrollPage(0, -1)} style={{marginLeft:"30px"}}>↑</button>
                    <div>
                      <button onClick={e => t.fastScrollPage(-1, 0)}>←</button>
                      <button onClick={e => t.fastScrollPage(0, 1)}>↓</button>
                      <button onClick={e => t.fastScrollPage(1, 0)}>→</button>
                    </div>
                  </div>
                </td>
                <td style={{paddingLeft:"100px",paddingRight:"15px"}}>
                  Jump To Start/End
                </td>
                <td>
                  <div>
                    <button onClick={e => t.fastScrollStartEnd(0, -1)} style={{marginLeft:"30px"}}>↑</button>
                    <div>
                      <button onClick={e => t.fastScrollStartEnd(-1, 0)}>←</button>
                      <button onClick={e => t.fastScrollStartEnd(0, 1)}>↓</button>
                      <button onClick={e => t.fastScrollStartEnd(1, 0)}>→</button>
                    </div>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        }

        <div>
          {!t.data.selectedLevel.isMirrorJson() &&
            <>
              <button disabled={!state.selectedLevel.isDirty} onClick={e => t.saveLevel()}>Save</button>
              <button onClick={e => t.cssld()}>Force Dirty</button>
              <button onClick={e => t.unhideAll()}>Unhide All</button>
            </>
          }

          <input type="checkbox" checked={state.showInstanceLabels} onChange={e => {state.showInstanceLabels = e.target.checked; t.changeState();}} />Show labels |
          <input type="checkbox" checked={state.showWallPaths} onChange={e => {t.setShowWallPaths(e.target.checked);}} />Show wall paths&nbsp;
          <button title="regenerate" disabled={!state.showWallPaths} onClick={e => t.refreshShowWallPaths()}>⟳</button> |
          {!t.config.isProd && (
            <>
              <input type="checkbox" checked={state.snapCollision} onChange={e => {state.snapCollision = e.target.checked; t.changeState();}} />Snap Collision |
            </>
          )}
          Zoom Level: <input style={{width:"35px"}} type="number" value={t.getZoom().toString()} onChange={e => t.setZoom(e.target.valueAsNumber)} /> |
          Canvas Size:
            <NumberInput initialValue={t.canvasWidth} onSubmit={num => { t.changeCanvasWidth(num); }} /> x&nbsp;
            <NumberInput initialValue={t.canvasHeight} onSubmit={num => { t.changeCanvasHeight(num); }} /> | 
          Clicked Mouse Coords: {Math.round(t.levelCanvas.lastClickX)},{Math.round(t.levelCanvas.lastClickY)}
        </div>
        
        <div id="map-properties">
          <h3 style={{margin:0}}>Map data</h3>
          <div>
            Short Name: <input type="text" maxLength={14} value={state.selectedLevel.shortName ?? ""} onChange={e => {state.selectedLevel.shortName = e.target.value; t.cssld();}} /> |
            Display Name: <input type="text" maxLength={25} value={state.selectedLevel.displayName ?? ""} onChange={e => {state.selectedLevel.displayName = e.target.value; t.cssld();}} /> |
            Custom Map Download Url: <input type="text" maxLength={128} style={{"width":"350px"}} value={state.selectedLevel.customMapUrl ?? ""} onChange={e => {state.selectedLevel.customMapUrl = e.target.value; t.cssld();}} />
          </div>

          <div>  
            Map Size:
            <NumberInput initialValue={state.selectedLevel.width} onSubmit={num => { t.changeWidth(num); }} /> x&nbsp;
            <NumberInput initialValue={state.selectedLevel.height} onSubmit={num => { t.changeHeight(num); }} /> | 
            Mirror X: <NumberInput initialValue={state.selectedLevel.mirrorX} onSubmit={num => {state.selectedLevel.mirrorX = num; t.cssld();}} /> |
            <input type="checkbox" checked={state.selectedLevel.mirroredOnly ?? false} onChange={e => {state.selectedLevel.mirroredOnly = e.target.checked; t.cssld();}} />Mirrored Only |
            <input type="checkbox" checked={state.selectedLevel.mirrorMapImages ?? true} onChange={e => {state.selectedLevel.mirrorMapImages = e.target.checked; t.cssld();}} />Mirror Map Images |
            {/*Kill Y: <NumberInput initialValue={state.selectedLevel.killY} onSubmit={num => {state.selectedLevel.killY = num; t.cssld();}} /> |*/}
            {/*Water Y: <NumberInput initialValue={state.selectedLevel.waterY} onSubmit={num => {state.selectedLevel.waterY = num; t.cssld();}} /> |*/}
            {/*Max Players: <NumberInput initialValue={state.selectedLevel.maxPlayers} onSubmit={num => {state.selectedLevel.maxPlayers = Helpers.clamp(Math.round(num), 2, 16); t.cssld();}} />*/}
            <input type="checkbox" checked={state.selectedLevel.supportsLargeCam ?? false} onChange={e => {state.selectedLevel.supportsLargeCam = e.target.checked; t.cssld();}} /> Supports Large Cam |
            <input type="checkbox" checked={state.selectedLevel.defaultLargeCam ?? false} onChange={e => {state.selectedLevel.defaultLargeCam = e.target.checked; t.cssld();}} /> Default Large Cam |
            BG Color Hex: <input style={{width:"50px"}} maxLength={6} type="text" value={state.selectedLevel.bgColorHex ?? ""} onChange={e => {state.selectedLevel.bgColorHex = e.target.value; t.cssld();}} /> |
            <input type="checkbox" checked={state.selectedLevel.raceOnly ?? false} onChange={e => {state.selectedLevel.raceOnly = e.target.checked; t.cssld();}} /> Race only?
          </div>

          <div>
            Background Shader: <input type="text" value={state.selectedLevel.backgroundShader ?? ""} onChange={e => {state.selectedLevel.backgroundShader = e.target.value; t.cssld();}} />
            Bg. Shader Image: <input type="text" value={state.selectedLevel.backgroundShaderImage ?? ""} onChange={e => {state.selectedLevel.backgroundShaderImage = e.target.value; t.cssld();}} /> |
            Parallax Shader: <input type="text" value={state.selectedLevel.parallaxShader ?? ""} onChange={e => {state.selectedLevel.parallaxShader = e.target.value; t.cssld();}} />
            Px. Shader Image: <input type="text" value={state.selectedLevel.parallaxShaderImage ?? ""} onChange={e => {state.selectedLevel.parallaxShaderImage = e.target.value; t.cssld();}} />
          </div>

          <input type="checkbox" checked={state.showBackground} onChange={e => {state.showBackground = e.target.checked; t.redraw(true); t.changeState();}} />
          Background:
          <select style={{width: "200px"}} value={state.selectedLevel.backgroundPath} onChange={e => {t.onBackgroundChange(e.target.value); t.cssld();}}>
            <option key={-1} value=""></option>
            {t.availableBackgrounds.map((background, index) => (
              <option key={index} value={Helpers.getAssetPath(background.path)}>{ Helpers.getAssetPath(background.path) }</option>
            ))}
          </select>

          <input type="checkbox" checked={state.showForeground} onChange={e => {state.showForeground = e.target.checked; t.redraw(true); t.changeState();}} />
          Foreground:
          <select style={{width: "200px"}} value={state.selectedLevel.foregroundPath} onChange={e => {t.onForegroundChange(e.target.value); t.cssld();}}>
            <option key={-1} value=""></option>
            {t.availableBackgrounds.map((background, index) => (
              <option key={index} value={Helpers.getAssetPath(background.path)}>{ Helpers.getAssetPath(background.path) }</option>
            ))}
          </select>
          
          <input type="checkbox" checked={state.showBackwall} onChange={e => {state.showBackwall = e.target.checked; t.redraw(true); t.changeState();}} />
          Backwall:
          <select style={{width: "200px"}} value={state.selectedLevel.backwallPath} onChange={e => {t.onBackwallChange(e.target.value); t.cssld();}}>
            <option key={-1} value=""></option>
            {t.availableBackgrounds.map((background, index) => (
              <option key={index} value={Helpers.getAssetPath(background.path)}>{ Helpers.getAssetPath(background.path) }</option>
            ))}
          </select>

          <br/>

          <input type="checkbox" checked={state.showParallaxes} onChange={e => {state.showParallaxes = e.target.checked; t.redraw(true); t.changeState();}} />
          Parallaxes:
          <button onClick={e => {state.selectedLevel.parallaxes.push(new Parallax()); t.cssld();}}>Add new</button>
          <div>
          {state.selectedLevel.parallaxes.map((parallax, index) => (
            <div className="parallax" key={parallax.path + "_" + index + "_" + parallax.isLargeCamOverride}>
              <div>
                <span>{ "Parallax " + (index + 1) }</span>
                <select style={{width: "250px"}} value={parallax.path} onChange={e => {t.onParallaxChange(index, e.target.value); t.cssld();}}>
                  <option key={-1} value=""></option>
                  {t.availableBackgrounds.map((background, index) => (
                    <option key={index} value={Helpers.getAssetPath(background.path)}>{ Helpers.getAssetPath(background.path) }</option>
                  ))}
                </select>
                <button onClick={e => {state.selectedLevel.parallaxes.splice(index, 1); t.cssld();}}>Remove</button>
                <button onClick={e => {t.moveParallax(index, -1);}}>Move Up</button>
                <button onClick={e => {t.moveParallax(index, 1);}}>Move Down</button>
                Is Large Cam Override? <input type="checkbox" checked={parallax.isLargeCamOverride} onChange={e => {parallax.isLargeCamOverride = e.target.checked; t.cssld();}} />
              </div>
              
              Parallax Start X<NumberInput initialValue={parallax.startX} onSubmit={num => {parallax.startX = num; t.cssld();} } />
              Parallax Start Y<NumberInput initialValue={parallax.startY} onSubmit={num => {parallax.startY = num; t.cssld();} } />
              Parallax X Speed<NumberInput initialValue={parallax.speedX} onSubmit={num => {parallax.speedX = num; t.cssld();} } />
              Parallax Y Speed<NumberInput initialValue={parallax.speedY} onSubmit={num => {parallax.speedY = num; t.cssld();} } />
              
              <br/>

              Parallax Mirror X: <NumberInput initialValue={parallax.mirrorX} onSubmit={num => {parallax.mirrorX = num; t.cssld();}} />
              Parallax Scroll Speed X: <NumberInput initialValue={parallax.scrollSpeedX} onSubmit={num => {parallax.scrollSpeedX = num; t.cssld();}} />
              Parallax Scroll Speed Y: <NumberInput initialValue={parallax.scrollSpeedY} onSubmit={num => {parallax.scrollSpeedY = num; t.cssld();}} />
            </div>
          ))}
          </div>
        </div>

        {
          state.selectedInstances.length > 0 &&
          <div id="instance-properties">
            <h3 style={{margin:0}}>Selected instance data</h3>
            <div className="properties">
              <div>
                <input type="checkbox" checked={!state.selectedInstances[0].hidden} onChange={e => {state.selectedInstances[0].hidden = !e.target.checked; t.changeState();}} />
                Name <input type="text" value={state.selectedInstances[0].name} onChange={e => {state.selectedInstances[0].rename(e.target.value, state.selectedLevel); t.cssld();}} />
              </div>
              <div>
                Object: {state.selectedInstances[0].objectName}
              </div>
              {state.selectedInstances[0].pos &&  
                <div>
                  x: <NumberInput initialValue={state.selectedInstances[0].pos.x} onSubmit={num => {state.selectedInstances[0].pos.x = num; t.cssld();}}/>
                  &nbsp;&nbsp;
                  y: <NumberInput initialValue={state.selectedInstances[0].pos.y} onSubmit={num => {state.selectedInstances[0].pos.y = num; t.cssld();}}/>
                </div>
              }

              Enabled in: 
              <select style={{width: "137px"}} value={state.selectedInstances[0].mirrorEnabled} onChange={e => {state.selectedInstances[0].mirrorEnabled = Number(e.target.value); t.cssld();}}>
                <option key={0} value={MirrorEnabled.Both}>Mirror+Non-Mirror</option>
                <option key={1} value={MirrorEnabled.NonMirroredOnly}>Non-Mirror Only</option>
                <option key={2} value={MirrorEnabled.MirroredOnly}>Mirror Only</option>
              </select>
              {
                state.selectedInstances[0].points &&
                <div>
                  Shape Instance Points:
                  {
                    state.selectedInstances[0].points.map((point, index) => (
                      <div key={String(point.x) + "," + String(point.y) + "," + index}>
                        <b>{index + 1})&nbsp;</b>
                        x: <NumberInput initialValue={point.x} onSubmit={num => {point.x = num; t.cssld();}}/>
                        &nbsp;&nbsp;
                        y: <NumberInput initialValue={point.y} onSubmit={num => {point.y = num; t.cssld();}}/>
                        {state.selectedInstances[0].points.length >= 4 && 
                          <button title="Delete" onClick={e => {state.selectedInstances[0].points.splice(index, 1); t.cssld();}}><img src="file:///images/delete.png" /></button>
                        }
                      </div>
                    ))
                  }
                </div>
              }
            </div>
            <div className="properties" style={{marginTop:-18,marginLeft:5,marginRight:5}}>
              <div>Properties:</div>
              <textarea rows={8} cols={40} 
                value={state.selectedInstances[0].getPropertiesString()} 
                onChange={e => {state.selectedInstances[0].propertiesString = e.target.value; t.forceUpdate();}} 
                onBlur={e => t.changeProperties(e.target.value)} />
            </div>

            <div className="properties">
            {
              state.selectedInstances[0].objectName.startsWith("Map Sprite") &&
              <>
                <div style={{display:"inline-block",marginRight:"5px"}}>
                  <PropertyInput propertyName="spriteName" value={t.getPropertyValue("spriteName") ?? ""} displayName="Sprite Name" levelEditor={t} options={t.getMapSpriteOptions()} />
                  <PropertyInput singleLine={true} propertyName="repeatX" value={t.getPropertyValue("repeatX") ?? 1} displayName="Repeat X" levelEditor={t} />
                  <PropertyInput singleLine={true} propertyName="repeatXPadding" value={t.getPropertyValue("repeatXPadding") ?? 1} displayName="X Padding" levelEditor={t} />
                  <br/>
                  <PropertyInput singleLine={true} propertyName="repeatY" value={t.getPropertyValue("repeatY") ?? 1} displayName="Repeat Y" levelEditor={t} />
                  <PropertyInput singleLine={true} propertyName="repeatYPadding" value={t.getPropertyValue("repeatYPadding") ?? 1} displayName="Y Padding" levelEditor={t} />
                  <PropertyInput propertyName="zIndex" value={t.getPropertyValue("zIndex") ?? "aboveBackground"} displayName="Z Index" levelEditor={t} options={["aboveForeground", "aboveInstances", "aboveBackground", "aboveBackwall", "aboveParallax"]} />
                  <PropertyInput propertyName="parallaxIndex" value={t.getPropertyValue("parallaxIndex") ?? 1} displayName="Parallax Index" levelEditor={t} />
                  <PropertyInput singleLine={true} propertyName="flipX" value={t.getPropertyValue("flipX") ?? true} displayName="Flip X" levelEditor={t} />
                  <PropertyInput singleLine={true} propertyName="flipY" value={t.getPropertyValue("flipY") ?? true} displayName="Flip Y" levelEditor={t} />
                  <PropertyInput singleLine={true} propertyName="doNotMirror" value={t.getPropertyValue("doNotMirror") ?? true} displayName="DoNotMirror" levelEditor={t} />
                </div>
                <div style={{display:"inline-block",verticalAlign:"top"}}>
                  <PropertyInput propertyName="destructableFlag" value={t.getPropertyValue("destructableFlag") ?? 1} displayName="Destructable Flag" levelEditor={t} options={[1,2,3]} />
                  <PropertyInput propertyName="destructableHealth" value={t.getPropertyValue("destructableHealth") ?? 12} displayName="Destructable Health" levelEditor={t} />
                  <PropertyInput propertyName="destroyInstanceName" value={t.getPropertyValue("destroyInstanceName") ?? ""} displayName="Destroy Instance" levelEditor={t} />
                  <PropertyInput propertyName="gibSpriteName" value={t.getPropertyValue("gibSpriteName") ?? ""} displayName="Gib Sprite" levelEditor={t} options={t.getMapSpriteOptions()} />
                </div>
              </>
            }

            {
              state.selectedInstances[0].objectName.startsWith("Moving Platform") &&
              <>
                <div style={{display:"inline-block",marginRight:"5px"}}>
                  <PropertyInput propertyName="spriteName" value={t.getPropertyValue("spriteName") ?? ""} displayName="Sprite Name" levelEditor={t} options={t.getMapSpriteOptions()} />
                  <PropertyInput propertyName="moveData" multiLineString={true} value={t.getPropertyValue("moveData") ?? ""} displayName="Move Data" levelEditor={t} />
                  <PropertyInput propertyName="moveSpeed" value={t.getPropertyValue("moveSpeed") ?? 50} displayName="Move Speed" levelEditor={t} />
                  <PropertyInput propertyName="timeOffset" value={t.getPropertyValue("timeOffset") ?? 0} displayName="Time Offset" levelEditor={t} />
                </div>
                <div style={{display:"inline-block",verticalAlign:"top"}}>
                  <PropertyInput propertyName="nodeName" value={t.getPropertyValue("nodeName") ?? ""} displayName="Node Name" levelEditor={t} />
                  <PropertyInput propertyName="killZoneName" value={t.getPropertyValue("killZoneName") ?? ""} displayName="Kill Zone Name" levelEditor={t} />
                  <PropertyInput propertyName="zIndex" value={t.getPropertyValue("zIndex") ?? "aboveBackground"} displayName="Z Index" levelEditor={t} options={["aboveForeground", "aboveInstances", "aboveBackground", "aboveBackwall", "aboveParallax"]} />
                  <PropertyInput propertyName="flipXOnMoveLeft" value={t.getPropertyValue("flipXOnMoveLeft") ?? true} displayName="Flip X On Move Left" levelEditor={t} />
                  <PropertyInput propertyName="flipYOnMoveUp" value={t.getPropertyValue("flipYOnMoveUp") ?? true} displayName="Flip Y On Move Up" levelEditor={t} />
                  <PropertyInput propertyName="idleSpriteName" value={t.getPropertyValue("idleSpriteName") ?? ""} displayName="Idle Sprite Name" levelEditor={t} options={t.getMapSpriteOptions()} />
                </div>
              </>
            }

            {
              state.selectedInstances[0].objectName.startsWith("Music Source") &&
              <>
                <div style={{display:"inline-block",marginRight:"5px"}}>
                  <PropertyInput propertyName="musicName" value={t.getPropertyValue("musicName") ?? ""} displayName="Music Name" levelEditor={t} />
                </div>
              </>
            }

            {
              state.selectedInstances[0].objectName.startsWith("Move Zone") &&
              <>
                <div style={{display:"inline-block",marginRight:"5px"}}>
                  <PropertyInput propertyName="moveX" value={t.getPropertyValue("moveX") ?? 0} displayName="Move X" levelEditor={t} />
                  <PropertyInput propertyName="moveY" value={t.getPropertyValue("moveY") ?? 0} displayName="Move Y" levelEditor={t} />
                </div>
              </>
            }

            {
              state.selectedInstances[0].objectName.startsWith("Turn Zone") &&
              <>
                <div style={{display:"inline-block",marginRight:"5px"}}>
                  <PropertyInput propertyName="turnDir" value={t.getPropertyValue("turnDir") ?? "left"} displayName="X Dir" levelEditor={t} options={["left","right"]} />
                  <PropertyInput propertyName="jumpAfterTurn" value={t.getPropertyValue("jumpAfterTurn") ?? true} displayName="Jump After Turn" levelEditor={t} />
                </div>
              </>
            }

            {
              state.selectedInstances[0].objectName.startsWith("Brake Zone") &&
              <>
                <div style={{display:"inline-block",marginRight:"5px"}}>
                </div>
              </>
            }

            {
              state.selectedInstances[0].objectName === "Control Point" &&
              <>
                <PropertyInput propertyName="hill" value={t.getPropertyValue("hill") ?? true} displayName="Hill" levelEditor={t} />
                <PropertyInput propertyName="num" value={t.getPropertyValue("num") ?? 1} displayName="CP Num" levelEditor={t} options={[1, 2]} />
                <PropertyInput propertyName="captureTime" value={t.getPropertyValue("captureTime") ?? 30} displayName="Time to Capture" levelEditor={t} />
                <PropertyInput propertyName="awardTime" value={t.getPropertyValue("awardTime") ?? 120} displayName="Time Awarded" levelEditor={t} />
              </>
            }
            
            {
              state.selectedInstances[0].objectName === 'No Scroll' &&
              <>
                <PropertyInput propertyName="freeDir" value={t.getPropertyValue("freeDir") ?? "left"} displayName="Free Dir" levelEditor={t} options={["left", "right", "up", "down"]} />
                <PropertyInput propertyName="snap" value={t.getPropertyValue("snap") ?? true} displayName="snap" levelEditor={t} />
              </>
            }

            {
              state.selectedInstances[0].objectName === 'Backwall Zone' &&
              <>
                <PropertyInput propertyName="isExclusion" value={t.getPropertyValue("isExclusion") ?? true} displayName="Is Exclusion" levelEditor={t} />
              </>
            }

            {
              state.selectedInstances[0].objectName === 'Jump Zone' &&
              <>
                <PropertyInput propertyName="forceDir" value={t.getPropertyValue("forceDir") ?? "left"} displayName="Force Dir" levelEditor={t} options={["left","right"]}/>
                <PropertyInput propertyName="jumpTime" value={t.getPropertyValue("jumpTime") ?? 0.5} displayName="Jump Time" levelEditor={t} />
              </>
            }

            {
              state.selectedInstances[0].objectName === 'Node' &&
              <>
                <PropertyInput propertyName="connectToSelfIfMirrored" value={t.getPropertyValue("connectToSelfIfMirrored") ?? true} displayName="Connect To Self If Mirrored" levelEditor={t} />
                {
                  (state.selectedInstances[0].properties.neighbors ?? []).map((neighbor: NavMeshNeighbor, index: number) => (
                    <div style={{display:"inline-block", border:"1px solid black", padding: "2px", margin: "2px"}} key={index + "_" + neighbor.nodeName}>
                      <div>
                        Neighbor: {neighbor.nodeName}
                        <button onClick={e => t.removeNeighbor(state.selectedInstances[0], neighbor.nodeName)}>🗑</button>
                      </div>
                      <PropertyInput propertyName="ladderDir" value={neighbor.ladderDir ?? "up"} displayName="Ladder Climb Dir" levelEditor={t} options={["up", "down"]} neighbor={neighbor} />
                      <PropertyInput propertyName="wallDir" value={neighbor.wallDir ?? "left"} displayName="Wall Climb Dir" levelEditor={t} options={["left", "right"]} neighbor={neighbor} />
                      <PropertyInput propertyName="platformJumpDir" value={neighbor.platformJumpDir ?? "left"} displayName="Platform Jump Dir" levelEditor={t} options={["left", "right"]} neighbor={neighbor} />
                      <PropertyInput propertyName="platformJumpDirDist" value={neighbor.platformJumpDirDist ?? 30} displayName="Platform Jump Dir Dist" levelEditor={t} neighbor={neighbor} />
                      <PropertyInput propertyName="includeJumpZones" value={neighbor.includeJumpZones ?? ""} displayName="Include Jump Zones" levelEditor={t} neighbor={neighbor} />
                      <PropertyInput propertyName="movingPlatXDist" value={neighbor.movingPlatXDist ?? 60} displayName="Moving Plat X Dist" levelEditor={t} neighbor={neighbor} />
                      <PropertyInput singleLine={true} propertyName="dash" value={neighbor.dash ?? true} displayName="Dash" levelEditor={t} neighbor={neighbor} />&nbsp;
                      <PropertyInput singleLine={true} propertyName="dropFromLadder" value={neighbor.dropFromLadder ?? true} displayName="Ladder Drop" levelEditor={t} neighbor={neighbor} />&nbsp;
                      <PropertyInput singleLine={true} propertyName="isDestNodeInAir" value={neighbor.isDestNodeInAir ?? true} displayName="To Air Node" levelEditor={t} neighbor={neighbor} />
                    </div>
                  ))
                }
              </>
            }

            {
              state.selectedInstances[0].objectName === 'Kill Zone' &&
              <>
                <PropertyInput propertyName="killInvuln" value={t.getPropertyValue("killInvuln") ?? true} displayName="Kill Invuln" levelEditor={t} />
                <PropertyInput propertyName="damage" value={t.getPropertyValue("damage") ?? 4} displayName="Override Damage" levelEditor={t} />
                <PropertyInput propertyName="flinch" value={t.getPropertyValue("flinch") ?? true} displayName="Flinch?" levelEditor={t} />
                <PropertyInput propertyName="hitCooldown" value={t.getPropertyValue("hitCooldown") ?? 1} displayName="Hit Cooldown" levelEditor={t} />
              </>
            }

            {
              state.selectedInstances[0].objectName === 'Collision Shape' &&
              <>
                <PropertyInput propertyName="boundary" value={t.getPropertyValue("boundary") ?? true} displayName="Boundary" levelEditor={t} />
                <PropertyInput propertyName="pitWall" value={t.getPropertyValue("pitWall") ?? true} displayName="Pit Wall" levelEditor={t} />
                <PropertyInput propertyName="unclimbable" value={t.getPropertyValue("unclimbable") ?? true} displayName="Unclimbable" levelEditor={t} />
                <PropertyInput propertyName="slippery" value={t.getPropertyValue("slippery") ?? true} displayName="Slippery" levelEditor={t} />
                <PropertyInput propertyName="topWall" value={t.getPropertyValue("topWall") ?? true} displayName="Top Wall" levelEditor={t} />
                <PropertyInput propertyName="moveX" value={t.getPropertyValue("moveX") ?? 0} displayName="Move X" levelEditor={t} />
              </>
            }

            {
              state.selectedInstances[0].objectName === 'Gate' &&
              <>
                <PropertyInput propertyName="unclimbable" value={t.getPropertyValue("unclimbable") ?? true} displayName="Unclimbable" levelEditor={t} />
              </>
            }

            {
              state.selectedInstances[0].objectName === 'Ride Armor' &&
              <>
                <PropertyInput propertyName="raType" value={t.getPropertyValue("raType") ?? "n"} displayName="Type" levelEditor={t} options={["n", "k", "h", "f"]} />
              </>
            }

            {
              state.selectedInstances[0].objectName === 'Ride Chaser' &&
              <>
                <PropertyInput propertyName="isCheckpoint" value={t.getPropertyValue("isCheckpoint") ?? true} displayName="Checkpoint Chaser?" levelEditor={t} />
              </>
            }

            {
              // Game Mode Exclusion Shared
              (state.selectedInstances[0].objectName === 'Ride Armor' || 
               state.selectedInstances[0].objectName === 'Ride Chaser' || 
               state.selectedInstances[0].objectName === 'Large Health' || 
               state.selectedInstances[0].objectName === 'Small Health' || 
               state.selectedInstances[0].objectName === 'Large Ammo' || 
               state.selectedInstances[0].objectName === 'Small Ammo') &&
              <>
                <PropertyInput propertyName="nonDmOnly" value={t.getPropertyValue("nonDmOnly") ?? true} displayName="Exclude in DM" levelEditor={t} />
                <PropertyInput propertyName="nonCtfOnly" value={t.getPropertyValue("nonCtfOnly") ?? true} displayName="Exclude in CTF" levelEditor={t} />
                <PropertyInput propertyName="nonKothOnly" value={t.getPropertyValue("nonKothOnly") ?? true} displayName="Exclude in KOTH" levelEditor={t} />
                <PropertyInput propertyName="nonCpOnly" value={t.getPropertyValue("nonCpOnly") ?? true} displayName="Exclude in CP" levelEditor={t} />
                <PropertyInput propertyName="dmOnly" value={t.getPropertyValue("dmOnly") ?? true} displayName="Include in DM Only" levelEditor={t} />
              </>
            }

            {
              // xDir Shared
              (
                state.selectedInstances[0].objectName === 'Ride Armor' ||
                state.selectedInstances[0].objectName === 'Ride Chaser' ||
                state.selectedInstances[0].objectName.includes('Spawn Point')
              ) &&
              <>
                <PropertyInput propertyName="flipX" value={t.getPropertyValue("flipX") ?? true} displayName="flipX" levelEditor={t} />
              </>
            }

            {
              (
                state.selectedInstances[0].objectName.includes('Spawn Point')
              ) &&
              <>
                <PropertyInput propertyName="raceStartSpawn" value={t.getPropertyValue("raceStartSpawn") ?? true} displayName="Race Start Spawn?" levelEditor={t} />
              </>
            }

            {
              (
                state.selectedInstances[0].objectName.includes('Goal')
              ) &&
              <>
                <PropertyInput propertyName="mirroredGoal" value={t.getPropertyValue("mirroredGoal") ?? true} displayName="Mirrored Goal?" levelEditor={t} />
              </>
            }

            </div>

          </div>
        }

      </div>
    }
  </div>
  );
}

function renderInstanceList(t: LevelEditor): JSX.Element {
  let state = t.data;
  return (
    <div className="sprite-list-container">
      <h1>Instances</h1>
      {
        state.selectedLevel &&
        <div>
          <button onClick={e => t.sortInstances()}>Sort</button>
          <div className="sprite-list-scroll">
            {
              state.selectedLevel.instances.map((instance, index) => (
                <div key={instance.id} onClick={e => t.onInstanceClick(instance)} style={{backgroundColor: instance.getListItemColor(state) }}>
                  { instance.name }
                </div>
              ))
            }
            <br/>
          </div>
        </div>
      }
    </div>
  );
}