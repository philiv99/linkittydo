import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PhraseDisplay } from '../components/PhraseDisplay';
import type { WordState } from '../types';

const createWords = (): WordState[] => [
  { index: 0, displayText: 'the', isHidden: false, isRevealed: false },
  { index: 1, displayText: null, isHidden: true, isRevealed: false },
  { index: 2, displayText: 'brown', isHidden: true, isRevealed: true },
  { index: 3, displayText: null, isHidden: true, isRevealed: false },
];

describe('PhraseDisplay', () => {
  it('renders all word slots', () => {
    render(
      <PhraseDisplay
        words={createWords()}
        onGuess={async () => false}
        onClue={() => {}}
      />
    );

    // Non-hidden word should show text
    expect(screen.getByText('the')).toBeInTheDocument();
    // Revealed hidden word should show text
    expect(screen.getByText('brown')).toBeInTheDocument();
  });

  it('shows give up button when onGiveUp provided', () => {
    render(
      <PhraseDisplay
        words={createWords()}
        onGuess={async () => false}
        onClue={() => {}}
        onGiveUp={() => {}}
      />
    );

    expect(screen.getByText('I Give Up')).toBeInTheDocument();
  });

  it('hides give up button when onGiveUp not provided', () => {
    render(
      <PhraseDisplay
        words={createWords()}
        onGuess={async () => false}
        onClue={() => {}}
      />
    );

    expect(screen.queryByText('I Give Up')).not.toBeInTheDocument();
  });
});
