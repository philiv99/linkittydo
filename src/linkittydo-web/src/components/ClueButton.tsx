import React from 'react';
import './ClueButton.css';

interface ClueButtonProps {
  onClick: () => void;
  disabled?: boolean;
}

export const ClueButton: React.FC<ClueButtonProps> = ({ onClick, disabled }) => {
  return (
    <button 
      className="clue-button" 
      onClick={onClick}
      disabled={disabled}
      title="Get a clue (opens in new tab)"
    >
      🔍
    </button>
  );
};
