import React from "react";

interface NumberInputProps {
  initialValue: number;
  onSubmit: (num: number) => void;
}

interface NumberInputState {
  currentValue: string;
  isFocused: boolean;
}

export class NumberInput extends React.Component<NumberInputProps, NumberInputState> {

  lastChangeWasDifferent: boolean;
  constructor(props: NumberInputProps) {
    super(props);
    this.state = {
      currentValue: this.props.initialValue?.toString() ?? "",
      isFocused: false,
    };
  }

  static getDerivedStateFromProps(props: NumberInputProps, state: NumberInputState): NumberInputState {
    if (!state.isFocused) {
      return {
        currentValue: props.initialValue?.toString() ?? "",
        isFocused: false,
      };
    }
    return {
      ...state
    }
  }

  handleOnChange(rawValue: string, numValue: number) {
    if (!isNaN(numValue)) {
      //this.props.onSubmit(numValue);
    }
    this.lastChangeWasDifferent = (rawValue !== this.state.currentValue);
    this.setState({
      currentValue: rawValue ?? "",
      isFocused: true,
    });
  }

  handleOnBlur(rawValue: string, numValue: number) {
    if (isNaN(numValue)) {
      numValue = 0;
      rawValue = "0";
    }
    if (this.lastChangeWasDifferent) {
      this.props.onSubmit(numValue);
      this.lastChangeWasDifferent = false;
    }
    this.setState({
      currentValue: rawValue ?? "",
      isFocused: false,
    });
  }

  incrementNumber(amount: number) {
    let currentNumber = parseInt(this.state.currentValue);
    if (isNaN(currentNumber)) currentNumber = 0;
    currentNumber += amount;
    this.props.onSubmit(currentNumber);
    this.setState({
      currentValue: currentNumber.toString(),
      isFocused: false,
    });
  }

  render() {
    return <div style={{display:"inline-block"}}>
      <input type="number" className="number-input" value={this.state.currentValue} 
      onChange={e => this.handleOnChange(e.target.value, e.target.valueAsNumber) } 
      onBlur={e => this.handleOnBlur(e.target.value, e.target.valueAsNumber) } />
      <span className="number-input-buttons">
        <button tabIndex={-1} className="number-input-button" onClick={e => this.incrementNumber(1)}>▲</button>
        <button tabIndex={-1} className="number-input-button" onClick={e => this.incrementNumber(-1)}>▼</button>
      </span>
    </div>
  }
}