import React, { useState } from 'react';
import { useGame } from '../hooks/useGame';
import { PhraseDisplay } from './PhraseDisplay';
import { ScoreDisplay } from './ScoreDisplay';
import { CluePanel, type ClueTab } from './CluePanel';
import './GameBoard.css';

export const GameBoard: React.FC = () => {
  const { gameState, loading, error, startGame, submitGuess, getClue } = useGame();
  const [clueTabs, setClueTabs] = useState<ClueTab[]>([]);
  const [activeTabId, setActiveTabId] = useState<string | null>(null);

  const handleGuess = async (wordIndex: number, guess: string): Promise<boolean> => {
    const response = await submitGuess(wordIndex, guess);
    return response?.isCorrect ?? false;
  };

  const handleClue = async (wordIndex: number) => {
    const url = await getClue(wordIndex);
    if (url) {
      const tabId = `clue-${Date.now()}`;
      const domain = new URL(url).hostname.replace('www.', '');
      const newTab: ClueTab = {
        id: tabId,
        title: domain,
        url: url,
        wordIndex: wordIndex
      };
      setClueTabs(prev => [...prev, newTab]);
      setActiveTabId(tabId);
    }
  };

  const handleTabClose = (tabId: string) => {
    setClueTabs(prev => {
      const newTabs = prev.filter(t => t.id !== tabId);
      if (activeTabId === tabId && newTabs.length > 0) {
        setActiveTabId(newTabs[newTabs.length - 1].id);
      } else if (newTabs.length === 0) {
        setActiveTabId(null);
      }
      return newTabs;
    });
  };

  const handleNewGame = () => {
    setClueTabs([]);
    setActiveTabId(null);
    startGame();
  };

  // Welcome screen - no game started
  if (!gameState && !loading) {
    return (
      <div className="splash-screen">
        <img src="/lounge.jpg" alt="LinkittyDo!" className="splash-logo" />
        <div className="splash-overlay">
          <button className="play-button" onClick={handleNewGame}>
            <span className="play-icon">▶</span>
            <span className="play-text">PLAY!</span>
          </button>
          {error && <p className="error">{error}</p>}
        </div>
      </div>
    );
  }

  // Loading state
  if (loading) {
    return (
      <div className="game-board">
        <div className="loading">Loading...</div>
      </div>
    );
  }

  // Game in progress
  if (gameState) {
    return (
      <div className="game-layout">
        <div className="game-board">
          <header className="game-header">
            <h1>LinkittyDo!</h1>
            <ScoreDisplay score={gameState.score} />
          </header>

          <main className="game-main">
            {gameState.isComplete ? (
              <div className="victory">
                <h2>Congratulations!</h2>
                <p>You completed the phrase!</p>
                <PhraseDisplay 
                  words={gameState.words} 
                  onGuess={handleGuess} 
                  onClue={handleClue} 
                />
                <p className="final-score">Final Score: {gameState.score}</p>
                <button className="start-button" onClick={handleNewGame}>
                  Play Again
                </button>
              </div>
            ) : (
              <PhraseDisplay 
                words={gameState.words} 
                onGuess={handleGuess} 
                onClue={handleClue} 
              />
            )}
          </main>

          <footer className="game-footer">
            <p>Type your guess and press Enter • Click the clue button for hints!</p>
          </footer>
        </div>

        <CluePanel 
          tabs={clueTabs}
          activeTabId={activeTabId}
          onTabSelect={setActiveTabId}
          onTabClose={handleTabClose}
        />
      </div>
    );
  }

  return null;
};
