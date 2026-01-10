import React from 'react';
import type { WordState } from '../types';
import { WordSlot } from './WordSlot';
import './PhraseDisplay.css';

interface PhraseDisplayProps {
  words: WordState[];
  onGuess: (wordIndex: number, guess: string) => Promise<boolean>;
  onClue: (wordIndex: number) => void;
}

export const PhraseDisplay: React.FC<PhraseDisplayProps> = ({ words, onGuess, onClue }) => {
  return (
    <div className="phrase-display">
      {words.map((word) => (
        <WordSlot
          key={word.index}
          word={word}
          onGuess={onGuess}
          onClue={onClue}
        />
      ))}
    </div>
  );
};
