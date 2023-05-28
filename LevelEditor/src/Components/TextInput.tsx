import React from "react";

interface TextInputProps {
  width: string;
  initialValue: string;
  isMultiLine?: boolean;
  onSubmit: (str: string) => void;
}

interface TextInputState {
  currentValue: string;
  isFocused: boolean;
}

export class TextInput extends React.Component<TextInputProps, TextInputState> {

  constructor(props: TextInputProps) {
    super(props);
    this.state = {
      currentValue: this.props.initialValue,
      isFocused: false
    };
  }

  static getDerivedStateFromProps(props: TextInputProps, state: TextInputState): TextInputState {
    if (!state.isFocused) {
      return {
        currentValue: props.initialValue,
        isFocused: false
      };
    }
    return {
      currentValue: state.currentValue,
      isFocused: state.isFocused
    }
  }

  handleOnChange(strValue: string) {
    this.setState({
      currentValue: strValue,
      isFocused: true
    });
  }

  handleOnBlur(strValue: string) {
    this.props.onSubmit(strValue);
    this.setState({
      currentValue: strValue,
      isFocused: false
    });
  }

  render() {
    if (this.props.isMultiLine) {
      return <textarea rows={4} cols={20} value={this.state.currentValue} style={{width:this.props.width}}
        onChange={e => this.handleOnChange(e.target.value) } 
        onBlur={e => this.handleOnBlur(e.target.value) } />
    }
    else {
      return <input type="text" value={this.state.currentValue} style={{width:this.props.width}}
        onChange={e => this.handleOnChange(e.target.value) } 
        onBlur={e => this.handleOnBlur(e.target.value) } />
    }
  }
}