import React from 'react';
import type { WordState } from '../types';
import { WordSlot } from './WordSlot';
import './PhraseDisplay.css';

interface PhraseDisplayProps {
  words: WordState[];
  onGuess: (wordIndex: number, guess: string) => Promise<boolean>;
  onClue: (wordIndex: number) => void;
  onGiveUp?: () => void;
}

export const PhraseDisplay: React.FC<PhraseDisplayProps> = ({ words, onGuess, onClue, onGiveUp }) => {
  return (
    <div className="phrase-display">
      <div className="phrase-words">
        {words.map((word) => (
          <WordSlot
            key={word.index}
            word={word}
            words={words}
            onGuess={onGuess}
            onClue={onClue}
          />
        ))}
      </div>
      {onGiveUp && (
        <button className="give-up-button" onClick={onGiveUp}>
          I Give Up 🏳️
        </button>
      )}
    </div>
  );
};
