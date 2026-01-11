import React, { useState } from 'react';
import type { WordState } from '../types';
import { GuessInput } from './GuessInput';
import { ClueButton } from './ClueButton';
import './WordSlot.css';

interface WordSlotProps {
  word: WordState;
  words: WordState[];
  onGuess: (wordIndex: number, guess: string) => Promise<boolean>;
  onClue: (wordIndex: number) => void;
}

// Calculate the 1-based position among only guessable (hidden) words
const getGuessablePosition = (wordIndex: number, words: WordState[]): number => {
  let position = 0;
  for (let i = 0; i <= wordIndex && i < words.length; i++) {
    if (words[i].isHidden) {
      position++;
    }
  }
  return position;
};

export const WordSlot: React.FC<WordSlotProps> = ({ word, words, onGuess, onClue }) => {
  const [isIncorrect, setIsIncorrect] = useState(false);

  // If word is not hidden, or is revealed, show the text
  if (!word.isHidden || word.isRevealed) {
    return (
      <span className={`word-slot revealed ${word.isHidden ? 'was-hidden' : ''}`}>
        {word.displayText}
      </span>
    );
  }

  // Hidden word - show input and clue button
  const handleGuess = async (guess: string) => {
    setIsIncorrect(false);
    const correct = await onGuess(word.index, guess);
    if (!correct) {
      setIsIncorrect(true);
      // Clear incorrect state after animation
      setTimeout(() => setIsIncorrect(false), 500);
    }
  };

  const handleClue = () => {
    onClue(word.index);
  };

  const position = getGuessablePosition(word.index, words);

  return (
    <span className="word-slot hidden">
      <span className="word-position">#{position}</span>
      <GuessInput onGuess={handleGuess} isIncorrect={isIncorrect} />
      <ClueButton onClick={handleClue} />
    </span>
  );
};
