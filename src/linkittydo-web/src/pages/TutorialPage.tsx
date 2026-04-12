import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './TutorialPage.css';

const TUTORIAL_STEPS = [
  {
    title: 'Welcome to LinkittyDo!',
    description: 'LinkittyDo is a word-guessing game where every clue is a web link. Your goal is to figure out a hidden phrase by reading the pages behind the links.',
    visual: 'phrase-example',
  },
  {
    title: 'The Phrase',
    description: 'You\'ll see a phrase with some words hidden. The visible words (like "the", "a", "is") help you understand context. The hidden words are what you need to guess.',
    visual: 'hidden-words',
  },
  {
    title: 'Getting Clues',
    description: 'Click the "Get Clue" button next to any hidden word. A web page will open that contains a hint about the word. Read it carefully — the page content relates to the hidden word through synonyms or associations.',
    visual: 'clue-button',
  },
  {
    title: 'Making a Guess',
    description: 'Once you think you know a word, click on it and type your guess. If you\'re correct, the word is revealed and you earn points! Fewer clues and guesses mean more points.',
    visual: 'guess-input',
  },
  {
    title: 'Scoring',
    description: 'Points are awarded based on difficulty, clue usage, and guess attempts. Getting a word right on your first try with no clues earns a 2x bonus! Try to maximize your score.',
    visual: 'scoring',
  },
  {
    title: 'Ready to Play!',
    description: 'That\'s all you need to know! Start a game, explore the clue links, and piece together the phrase. Good luck!',
    visual: 'ready',
  },
];

export const TutorialPage: React.FC = () => {
  const [currentStep, setCurrentStep] = useState(0);
  const navigate = useNavigate();

  const step = TUTORIAL_STEPS[currentStep];
  const isLastStep = currentStep === TUTORIAL_STEPS.length - 1;
  const isFirstStep = currentStep === 0;

  const handleNext = () => {
    if (isLastStep) {
      localStorage.setItem('linkittydo_tutorial_seen', 'true');
      navigate('/play');
    } else {
      setCurrentStep(currentStep + 1);
    }
  };

  const handlePrev = () => {
    if (!isFirstStep) {
      setCurrentStep(currentStep - 1);
    }
  };

  const handleSkip = () => {
    localStorage.setItem('linkittydo_tutorial_seen', 'true');
    navigate('/play');
  };

  return (
    <div className="tutorial-page">
      <div className="tutorial-container">
        <div className="tutorial-progress">
          {TUTORIAL_STEPS.map((_, index) => (
            <div
              key={index}
              className={`tutorial-progress-dot ${index === currentStep ? 'active' : ''} ${index < currentStep ? 'completed' : ''}`}
            />
          ))}
        </div>

        <div className="tutorial-content">
          <h2 className="tutorial-title">{step.title}</h2>
          
          <div className={`tutorial-visual tutorial-visual-${step.visual}`}>
            {step.visual === 'phrase-example' && (
              <div className="tutorial-demo">
                <div className="demo-phrase">
                  <span className="demo-word visible">breaking</span>
                  <span className="demo-word hidden">???</span>
                </div>
                <div className="demo-hint">The phrase has 2 words. One is visible, one is hidden.</div>
              </div>
            )}
            {step.visual === 'hidden-words' && (
              <div className="tutorial-demo">
                <div className="demo-phrase">
                  <span className="demo-word visible">the</span>
                  <span className="demo-word hidden">???</span>
                  <span className="demo-word visible">is</span>
                  <span className="demo-word hidden">???</span>
                </div>
                <div className="demo-hint">Small common words stay visible to give you context.</div>
              </div>
            )}
            {step.visual === 'clue-button' && (
              <div className="tutorial-demo">
                <div className="demo-clue-area">
                  <span className="demo-word hidden">???</span>
                  <button className="demo-clue-btn" disabled>Get Clue</button>
                </div>
                <div className="demo-hint">Each clue opens a real web page with hints about the word.</div>
              </div>
            )}
            {step.visual === 'guess-input' && (
              <div className="tutorial-demo">
                <div className="demo-guess-area">
                  <span className="demo-word hidden">???</span>
                  <div className="demo-input-row">
                    <input className="demo-input" value="news" disabled />
                    <button className="demo-submit-btn" disabled>Guess</button>
                  </div>
                </div>
                <div className="demo-hint">Type your guess and submit. Correct answers reveal the word!</div>
              </div>
            )}
            {step.visual === 'scoring' && (
              <div className="tutorial-demo">
                <div className="demo-score-table">
                  <div className="demo-score-row">
                    <span>First guess, no clues</span>
                    <span className="demo-points">200 pts</span>
                  </div>
                  <div className="demo-score-row">
                    <span>With 1 clue</span>
                    <span className="demo-points">100 pts</span>
                  </div>
                  <div className="demo-score-row">
                    <span>Multiple attempts</span>
                    <span className="demo-points">50 pts</span>
                  </div>
                </div>
                <div className="demo-hint">Fewer clues and guesses = higher score!</div>
              </div>
            )}
            {step.visual === 'ready' && (
              <div className="tutorial-demo">
                <div className="demo-ready">
                  <div className="demo-ready-icon">🎮</div>
                  <div className="demo-hint">Click "Start Playing" to begin your first game!</div>
                </div>
              </div>
            )}
          </div>

          <p className="tutorial-description">{step.description}</p>
        </div>

        <div className="tutorial-actions">
          <button
            className="tutorial-btn tutorial-btn-skip"
            onClick={handleSkip}
          >
            Skip Tutorial
          </button>
          <div className="tutorial-btn-group">
            {!isFirstStep && (
              <button
                className="tutorial-btn tutorial-btn-prev"
                onClick={handlePrev}
              >
                Back
              </button>
            )}
            <button
              className="tutorial-btn tutorial-btn-next"
              onClick={handleNext}
            >
              {isLastStep ? 'Start Playing' : 'Next'}
            </button>
          </div>
        </div>

        <div className="tutorial-step-counter">
          Step {currentStep + 1} of {TUTORIAL_STEPS.length}
        </div>
      </div>
    </div>
  );
};
