import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { TutorialPage } from '../pages/TutorialPage';

const renderTutorial = () =>
  render(
    <MemoryRouter>
      <TutorialPage />
    </MemoryRouter>
  );

describe('TutorialPage', () => {
  it('renders the first step on load', () => {
    renderTutorial();
    expect(screen.getByText('Welcome to LinkittyDo!')).toBeInTheDocument();
  });

  it('shows Next button on first step', () => {
    renderTutorial();
    expect(screen.getByText('Next')).toBeInTheDocument();
  });

  it('does not show Back button on first step', () => {
    renderTutorial();
    expect(screen.queryByText('Back')).not.toBeInTheDocument();
  });

  it('advances to next step on Next click', () => {
    renderTutorial();
    fireEvent.click(screen.getByText('Next'));
    expect(screen.getByText('The Phrase')).toBeInTheDocument();
  });

  it('shows Back button after advancing', () => {
    renderTutorial();
    fireEvent.click(screen.getByText('Next'));
    expect(screen.getByText('Back')).toBeInTheDocument();
  });

  it('goes back to previous step on Back click', () => {
    renderTutorial();
    fireEvent.click(screen.getByText('Next'));
    fireEvent.click(screen.getByText('Back'));
    expect(screen.getByText('Welcome to LinkittyDo!')).toBeInTheDocument();
  });

  it('shows Skip Tutorial link', () => {
    renderTutorial();
    expect(screen.getByText('Skip Tutorial')).toBeInTheDocument();
  });

  it('renders progress indicators', () => {
    const { container } = renderTutorial();
    const dots = container.querySelectorAll('.tutorial-progress-dot');
    expect(dots.length).toBeGreaterThan(0);
  });

  it('shows Start Playing on last step', () => {
    renderTutorial();
    // Advance through all steps
    const totalSteps = 6;
    for (let i = 0; i < totalSteps - 1; i++) {
      fireEvent.click(screen.getByText('Next'));
    }
    expect(screen.getByText("Start Playing")).toBeInTheDocument();
  });
});
