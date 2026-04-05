import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ScoreDisplay } from '../components/ScoreDisplay';

describe('ScoreDisplay', () => {
  it('renders the score value', () => {
    render(<ScoreDisplay score={250} />);

    expect(screen.getByText('250')).toBeInTheDocument();
    expect(screen.getByText('Score:')).toBeInTheDocument();
  });

  it('renders zero score', () => {
    render(<ScoreDisplay score={0} />);

    expect(screen.getByText('0')).toBeInTheDocument();
  });
});
