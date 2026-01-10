import React, { useState } from 'react';
import './GuessInput.css';

interface GuessInputProps {
  onGuess: (guess: string) => void;
  disabled?: boolean;
  isIncorrect?: boolean;
}

export const GuessInput: React.FC<GuessInputProps> = ({ onGuess, disabled, isIncorrect }) => {
  const [value, setValue] = useState('');

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && value.trim()) {
      onGuess(value.trim());
      // Don't clear on incorrect - let user see what they typed
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setValue(e.target.value);
  };

  return (
    <input
      type="text"
      className={`guess-input ${isIncorrect ? 'incorrect' : ''}`}
      value={value}
      onChange={handleChange}
      onKeyDown={handleKeyDown}
      disabled={disabled}
      placeholder="?"
      autoComplete="off"
    />
  );
};
