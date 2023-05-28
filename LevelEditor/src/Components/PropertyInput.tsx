import React from "react";
import { LevelEditor } from "../levelEditor/levelEditor";
import { NavMeshNeighbor } from "../models/NavMeshNode";
import { NumberInput } from "./NumberInput";
import { TextInput } from "./TextInput";

export enum PropertyInputType {
  Boolean,
  Number,
  String,
  MultiLineString,
}

interface PropertyInputProps {
  propertyName: string;
  displayName: string;
  levelEditor: LevelEditor;
  value: any;
  options?: any[];
  neighbor?: NavMeshNeighbor;
  singleLine?: boolean;
  multiLineString?: boolean;
  onChangeCallback?: (newValue: any) => void;
}

export class PropertyInput extends React.Component<PropertyInputProps, {}> {
  
  propertyInputType: PropertyInputType;

  constructor(props: PropertyInputProps) {
    super(props);
    this.state = {};
    if (typeof props.value === "number") {
      this.propertyInputType = PropertyInputType.Number;
    }
    else if (typeof props.value === "boolean") {
      this.propertyInputType = PropertyInputType.Boolean;
    }
    else if (typeof props.value === "string") {
      this.propertyInputType = props.multiLineString ? PropertyInputType.MultiLineString : PropertyInputType.String;
    }
  }

  onChangeValue(value: any) {
    let p = this.props;
    let t = this.props.levelEditor;
    if (p.neighbor) {
      t.setNeighborProperty(p.neighbor, p.propertyName, value, false);
    }
    else {
      t.setProperty(p.propertyName, value);
    }
    if (p.onChangeCallback) {
      p.onChangeCallback(value);
    }
  }

  onClickButton() {
    let p = this.props;
    let t = this.props.levelEditor;
    if (p.neighbor) {
      t.setNeighborProperty(p.neighbor, p.propertyName, p.value, true);
    }
    else {
      if (t.propertyExists(p.propertyName)) {
        t.removeProperty(p.propertyName);
      }
      else {
        t.setProperty(p.propertyName, p.value);
      }
    }
  }

  convertValue(value: any) {
    switch (this.propertyInputType) {
      case PropertyInputType.Number: return Number(value);
      case PropertyInputType.Boolean: return Boolean(value);
      case PropertyInputType.String: return String(value);
      default: return value;
    }
  }

  render() {
    let p = this.props;
    let t = this.props.levelEditor;
    return <div style={{display: p.singleLine ? "inline-block" : "block"}}>
      <button 
        style={{verticalAlign:"top"}}
        className={p.neighbor ? t.getNeighborButtonStyle(p.neighbor, p.propertyName) : t.getPropertyButtonStyle(p.propertyName)} 
        onClick={e => this.onClickButton()}>{p.displayName}</button>
      {
        p.options &&
        <select style={{maxWidth:"175px"}} value={p.value} onChange={e => { this.onChangeValue(this.convertValue(e.target.value)); } }>
          {p.options.map((option, index) => (
            <option key={index} value={option}>{option}</option>
          ))}
        </select>
      }
      {
        !p.options && this.propertyInputType === PropertyInputType.Number &&
        <NumberInput initialValue={p.value} onSubmit={num => this.onChangeValue(num)} />
      }
      {
        !p.options && this.propertyInputType === PropertyInputType.String &&
        <TextInput width="75px" initialValue={p.value} onSubmit={str => this.onChangeValue(str)} />
      }
      {
        !p.options && this.propertyInputType === PropertyInputType.MultiLineString &&
        <TextInput isMultiLine={true} width="150px" initialValue={p.value} onSubmit={str => this.onChangeValue(str)} />
      }
    </div>
  }

}