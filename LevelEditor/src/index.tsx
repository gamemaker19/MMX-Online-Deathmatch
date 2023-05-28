import 'reflect-metadata';
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { SpriteEditor } from './spriteEditor/spriteEditor';
import { LevelEditor } from './levelEditor/levelEditor';

async function start() {
  let type: number = await window.Main.getEditorType();
  if (type === 0) { 
    ReactDOM.render(
      <SpriteEditor />,
      document.getElementById('root')
    );
  }
  else {
    ReactDOM.render(
      <LevelEditor />,
      document.getElementById('root')
    );
  }
}

start();
