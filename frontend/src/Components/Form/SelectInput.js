import PropTypes from 'prop-types';
import React, { Component } from 'react';
import classNames from 'classnames';
import styles from './SelectInput.css';

class SelectInput extends Component {

  //
  // Listeners

  onChange = (event) => {
    const selected = Array.from(event.target.selectedOptions, o => o.value);
    this.props.onChange({
      name: this.props.name,
      value: this.props.multiple ? selected : (selected[0] ? selected[0] : ''),
    });
  }

  //
  // Render

  render() {
    const {
      className,
      disabledClassName,
      name,
      value,
      values,
      isDisabled,
      hasError,
      hasWarning,
      autoFocus,
      onBlur,
      multiple,
      style
    } = this.props;

    return (
      <select
        className={classNames(
          className,
          hasError && styles.hasError,
          hasWarning && styles.hasWarning,
          isDisabled && disabledClassName
        )}
        disabled={isDisabled}
        name={name}
        value={value}
        autoFocus={autoFocus}
        onChange={this.onChange}
        onBlur={onBlur}
        multiple={multiple}
        style={style}
      >
        {
          values.map((option) => {
            const {
              key,
              value: optionValue,
              ...otherOptionProps
            } = option;

            return (
              <option
                key={key}
                value={key}
                {...otherOptionProps}
              >
                {optionValue}
              </option>
            );
          })
        }
      </select>
    );
  }
}

SelectInput.propTypes = {
  className: PropTypes.string,
  style: PropTypes.object,
  disabledClassName: PropTypes.string,
  name: PropTypes.string.isRequired,
  multiple: PropTypes.bool,
  value: PropTypes.oneOfType([PropTypes.string, PropTypes.number, PropTypes.arrayOf(PropTypes.string), PropTypes.arrayOf(PropTypes.number)]).isRequired,
  values: PropTypes.arrayOf(PropTypes.object).isRequired,
  isDisabled: PropTypes.bool,
  hasError: PropTypes.bool,
  hasWarning: PropTypes.bool,
  autoFocus: PropTypes.bool.isRequired,
  onChange: PropTypes.func.isRequired,
  onBlur: PropTypes.func
};

SelectInput.defaultProps = {
  className: styles.select,
  disabledClassName: styles.isDisabled,
  isDisabled: false,
  autoFocus: false,
  multiple: false
};

export default SelectInput;
