import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { WordSlot } from '../components/WordSlot';
import type { WordState } from '../types';

const allWords: WordState[] = [
  { index: 0, displayText: 'the', isHidden: false, isRevealed: false },
  { index: 1, displayText: null, isHidden: true, isRevealed: false },
  { index: 2, displayText: 'fox', isHidden: true, isRevealed: true },
];

describe('WordSlot', () => {
  it('renders revealed text for non-hidden words', () => {
    const word: WordState = { index: 0, displayText: 'the', isHidden: false, isRevealed: false };

    render(<WordSlot word={word} words={allWords} onGuess={async () => false} onClue={() => {}} />);

    expect(screen.getByText('the')).toBeInTheDocument();
  });

  it('renders revealed text for correctly guessed hidden words', () => {
    const word: WordState = { index: 2, displayText: 'fox', isHidden: true, isRevealed: true };

    render(<WordSlot word={word} words={allWords} onGuess={async () => false} onClue={() => {}} />);

    expect(screen.getByText('fox')).toBeInTheDocument();
  });

  it('shows position number for hidden unrevealed words', () => {
    const word: WordState = { index: 1, displayText: null, isHidden: true, isRevealed: false };

    render(<WordSlot word={word} words={allWords} onGuess={async () => false} onClue={() => {}} />);

    expect(screen.getByText('#1')).toBeInTheDocument();
  });
});
