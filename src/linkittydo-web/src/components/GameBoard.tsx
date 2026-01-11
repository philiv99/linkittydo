import React, { useState } from 'react';
import { useGame } from '../hooks/useGame';
import { useAudioSequence } from '../hooks/useAudio';
import { useUser } from '../hooks/useUser';
import { api } from '../services/api';
import { PhraseDisplay } from './PhraseDisplay';
import { ScoreDisplay } from './ScoreDisplay';
import { CluePanel, type ClueTab } from './CluePanel';
import { UserModal } from './UserModal';
import { UserManageModal } from './UserManageModal';
import './GameBoard.css';

const POINTS_PER_WORD = 100;

export const GameBoard: React.FC = () => {
  const { gameState, loading, error, startGame, submitGuess, getClue, giveUp } = useGame();
  const { 
    user, 
    isGuest,
    allUsers,
    loading: userLoading, 
    error: userError, 
    registerUser, 
    switchUser,
    updateDifficulty,
    addPoints,
    checkNameAvailability, 
    checkEmailAvailability,
    signOut,
    clearError 
  } = useUser();
  const [clueTabs, setClueTabs] = useState<ClueTab[]>([]);
  const [activeTabId, setActiveTabId] = useState<string | null>(null);
  const [audioStarted, setAudioStarted] = useState(false);
  const [showUserModal, setShowUserModal] = useState(false);
  const [showManageModal, setShowManageModal] = useState(false);
  const [gaveUp, setGaveUp] = useState(false);
  const { playSequence, stopAll } = useAudioSequence();

  // Handle click to start audio
  const handleStartAudio = () => {
    if (!audioStarted) {
      playSequence();
      setAudioStarted(true);
    }
  };

  const handleGuess = async (wordIndex: number, guess: string): Promise<boolean> => {
    const response = await submitGuess(wordIndex, guess);
    if (response?.isCorrect) {
      // Add points for correct guess
      await addPoints(POINTS_PER_WORD);
      
      // If phrase is complete, add bonus points
      if (response.isPhraseComplete && gameState) {
        // Count guessable words (hidden words that can be guessed)
        const guessableWords = gameState.words.filter(w => w.isHidden).length;
        const bonusPoints = guessableWords * POINTS_PER_WORD;
        await addPoints(bonusPoints);
      }
    }
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
    setGaveUp(false);
    stopAll();
    // Pass userId for non-guest users so game events are tracked
    // Guest users (no email) don't have their games saved
    const startRequest = !isGuest ? { 
      userId: user.uniqueId,
      difficulty: user.preferredDifficulty 
    } : undefined;
    startGame(startRequest);
  };

  const handleSwitchUser = async (uniqueId: string): Promise<boolean> => {
    const success = await switchUser(uniqueId);
    if (success) {
      // Reset game state when switching users
      setClueTabs([]);
      setActiveTabId(null);
      setGaveUp(false);
      stopAll();
      // Note: We'll start the game when the user state is updated
      // Pass userId for non-guest users
      // Since we just switched, the user will no longer be a guest
      const switchedUser = allUsers.find(u => u.uniqueId === uniqueId);
      const startRequest = switchedUser ? { 
        userId: uniqueId,
        difficulty: switchedUser.preferredDifficulty 
      } : undefined;
      startGame(startRequest);
    }
    return success;
  };

  const handleGiveUp = async () => {
    setGaveUp(true);
    await giveUp();
  };

  // Click to start screen - need user interaction for audio
  if (!audioStarted && !gameState && !loading) {
    return (
      <div className="click-to-start" onClick={handleStartAudio}>
        <div className="click-content">
          <h1>🎵 LinkittyDo! 🎵</h1>
          <p>Click anywhere to start</p>
        </div>
      </div>
    );
  }

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

  // Loading state - show a proper full-screen loading overlay
  if (loading) {
    return (
      <div className="loading-screen">
        <div className="loading-content">
          <h1 className="loading-title">LinkittyDo!</h1>
          <div className="loading-spinner"></div>
          <p className="loading-text">Finding the perfect phrase...</p>
        </div>
      </div>
    );
  }

  // Game in progress
  if (gameState) {
    return (
      <div className="game-layout">
        <div className="game-board">
          <header className="game-header">
            <div className="header-title">
              <h1>LinkittyDo!</h1>
              <span 
                className="user-name user-name-clickable"
                onClick={() => isGuest ? setShowUserModal(true) : setShowManageModal(true)}
                title={isGuest ? 'Click to create a profile' : 'Click to manage account'}
              >
                Playing as {user.name}
                {isGuest && <span className="guest-badge">👤</span>}
              </span>
              <div className="lifetime-points" title="Lifetime Points">
                <span className="points-icon">⭐</span>
                <span className="points-value">{user.lifetimePoints.toLocaleString()}</span>
              </div>
            </div>
            <ScoreDisplay score={gameState.score} />
          </header>

          <main className="game-main">
            {gameState.isComplete ? (
              <div className="victory">
                <h2>{gaveUp ? 'Better luck next time!' : 'Congratulations!'}</h2>
                <p>{gaveUp ? 'The phrase was:' : 'You completed the phrase!'}</p>
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
                onGiveUp={handleGiveUp}
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
          words={gameState.words}
        />

        <UserModal
          isOpen={showUserModal}
          onClose={() => {
            setShowUserModal(false);
            clearError();
          }}
          onSubmit={async (name, email) => {
            const success = await registerUser({ name, email });
            if (success) {
              // Start a new game for the new user
              // We need to wait for the next user list fetch and get the new user ID
              // Since registerUser returns the user info and updates state, 
              // we should fetch the new user to get their ID for starting the game
              try {
                const newUserData = await api.getUserByEmail(email);
                if (newUserData) {
                  setClueTabs([]);
                  setActiveTabId(null);
                  setGaveUp(false);
                  stopAll();
                  startGame({ 
                    userId: newUserData.uniqueId, 
                    difficulty: newUserData.preferredDifficulty 
                  });
                }
              } catch (e) {
                // Fallback: start game without userId (guest mode)
                handleNewGame();
              }
            }
            return success;
          }}
          onCheckName={checkNameAvailability}
          onCheckEmail={checkEmailAvailability}
          onSelectExistingUser={handleSwitchUser}
          allUsers={allUsers}
          loading={userLoading}
          error={userError}
        />

        <UserManageModal
          isOpen={showManageModal}
          onClose={() => {
            setShowManageModal(false);
            clearError();
          }}
          currentUser={user}
          allUsers={allUsers}
          isGuest={isGuest}
          onSignOut={signOut}
          onSwitchUser={handleSwitchUser}
          onUpdateDifficulty={updateDifficulty}
          onCreateProfile={() => setShowUserModal(true)}
          loading={userLoading}
        />
      </div>
    );
  }

  return null;
};
