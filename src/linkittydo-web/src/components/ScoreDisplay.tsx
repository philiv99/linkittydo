import React from 'react';
import './ScoreDisplay.css';

interface ScoreDisplayProps {
  score: number;
}

export const ScoreDisplay: React.FC<ScoreDisplayProps> = ({ score }) => {
  return (
    <div className="score-display">
      <span className="score-label">Score:</span>
      <span className="score-value">{score}</span>
    </div>
  );
};
