import React, { useState, useRef, useEffect, useCallback } from 'react';
import { useGame } from '../hooks/useGame';
import { useAudioSequence } from '../hooks/useAudio';
import { useSoundEffects } from '../hooks/useSoundEffects';
import { useUser } from '../hooks/useUser';
import { api } from '../services/api';
import { PhraseDisplay } from './PhraseDisplay';
import { ScoreDisplay } from './ScoreDisplay';
import { CluePanel, type ClueTab } from './CluePanel';
import { UserModal } from './UserModal';
import { UserManageModal } from './UserManageModal';
import './GameBoard.css';

const POINTS_PER_WORD = 100;
const APP_VERSION = '0.1.0';
const formatTime = (seconds: number): string => {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, '0')}`;
};

export const GameBoard: React.FC = () => {
  const { gameState, loading, error, startGame, submitGuess, getClue, giveUp } = useGame();
  const { 
    user, 
    isGuest,
    allUsers,
    loading: userLoading, 
    error: userError, 
    registerUser, 
    loginUser,
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
  const [showUserModal, setShowUserModal] = useState(false);
  const [showManageModal, setShowManageModal] = useState(false);
  const [gaveUp, setGaveUp] = useState(false);
  const [streak, setStreak] = useState(0);
  const [showStreakAnimation, setShowStreakAnimation] = useState(false);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const { playSequence, stopAll } = useAudioSequence();
  const { playCorrect, playIncorrect, playSolved, playGaveUp, setMuted, isMuted } = useSoundEffects();
  const [soundMuted, setSoundMuted] = useState(isMuted());

  // Track if we've auto-started a game
  const hasAutoStartedRef = useRef(false);

  // Auto-start game on mount
  useEffect(() => {
    if (!gameState && !loading && !hasAutoStartedRef.current) {
      hasAutoStartedRef.current = true;
      playSequence();
      const startRequest = !isGuest ? { 
        userId: user.uniqueId,
        difficulty: user.preferredDifficulty 
      } : undefined;
      startGame(startRequest);
    }
    if (isGuest) {
      hasAutoStartedRef.current = false;
    }
  }, [isGuest, gameState, loading, user.uniqueId, user.preferredDifficulty, startGame, playSequence]);

  // Reset auto-start flag on error
  useEffect(() => {
    if (error && !isGuest) {
      hasAutoStartedRef.current = false;
    }
  }, [error, isGuest]);

  const handleGuess = async (wordIndex: number, guess: string): Promise<boolean> => {
    const response = await submitGuess(wordIndex, guess);
    if (response?.isCorrect) {
      playCorrect();
      setStreak(prev => prev + 1);
      setShowStreakAnimation(true);
      setTimeout(() => setShowStreakAnimation(false), 1000);
      await addPoints(POINTS_PER_WORD);
      
      if (response.isPhraseComplete && gameState) {
        playSolved();
        const guessableWords = gameState.words.filter(w => w.isHidden).length;
        const bonusPoints = guessableWords * POINTS_PER_WORD;
        await addPoints(bonusPoints);
      }
    } else {
      playIncorrect();
      setStreak(0);
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

  const handleNewGame = useCallback(() => {
    setClueTabs([]);
    setActiveTabId(null);
    setGaveUp(false);
    setStreak(0);
    setElapsedSeconds(0);
    stopAll();
    const startRequest = !isGuest ? { 
      userId: user.uniqueId,
      difficulty: user.preferredDifficulty 
    } : undefined;
    startGame(startRequest);
  }, [isGuest, user.uniqueId, user.preferredDifficulty, startGame, stopAll]);

  const handleSwitchUser = async (uniqueId: string): Promise<boolean> => {
    const success = await switchUser(uniqueId);
    if (success) {
      setClueTabs([]);
      setActiveTabId(null);
      setGaveUp(false);
      stopAll();
      const switchedUser = allUsers.find(u => u.uniqueId === uniqueId);
      const startRequest = switchedUser ? { 
        userId: uniqueId,
        difficulty: switchedUser.preferredDifficulty 
      } : undefined;
      startGame(startRequest);
    }
    return success;
  };

  const handleGiveUp = useCallback(async () => {
    setGaveUp(true);
    playGaveUp();
    await giveUp();
  }, [giveUp, playGaveUp]);

  const handleSignOut = () => {
    signOut();
    setClueTabs([]);
    setActiveTabId(null);
    setGaveUp(false);
    stopAll();
    startGame();
  };

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement;
      if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA') return;

      if (e.key === 'g' || e.key === 'G') {
        if (gameState && !gameState.isComplete && !gaveUp) {
          e.preventDefault();
          handleGiveUp();
        }
      } else if (e.key === 'n' || e.key === 'N') {
        if (gameState?.isComplete) {
          e.preventDefault();
          handleNewGame();
        }
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [gameState, gaveUp, handleGiveUp, handleNewGame]);

  // Game timer
  useEffect(() => {
    if (!gameState || gameState.isComplete) return;
    const interval = setInterval(() => {
      setElapsedSeconds(prev => prev + 1);
    }, 1000);
    return () => clearInterval(interval);
  }, [gameState, gameState?.isComplete]);

  // Loading state
  if (loading && !gameState) {
    return (
      <div className="game-layout">
        <div className="game-loading">
          <div className="loading-spinner"></div>
          <p>Finding the perfect phrase...</p>
        </div>
      </div>
    );
  }

  // Determine game status text
  const getStatusText = () => {
    if (!gameState) return 'Ready';
    if (gameState.isComplete) return gaveUp ? 'Game Over' : 'Solved!';
    const hidden = gameState.words.filter(w => w.isHidden && !w.isRevealed).length;
    return `${hidden} word${hidden !== 1 ? 's' : ''} remaining`;
  };

  return (
    <div className="game-layout">
      {/* Phrase Bar - compact top section */}
      <section className="phrase-bar">
        <div className="phrase-bar-content">
          {gameState ? (
            <>
              <div className="phrase-bar-stats">
                <ScoreDisplay score={gameState.score} />
                <div className="game-timer" aria-label={`Time: ${formatTime(elapsedSeconds)}`}>
                  <span className="timer-value">{formatTime(elapsedSeconds)}</span>
                </div>
                {streak > 1 && (
                  <div className={`streak-indicator ${showStreakAnimation ? 'streak-pop' : ''}`}>
                    <span className="streak-value">{streak}x</span>
                  </div>
                )}
              </div>
              <div className="phrase-bar-phrase">
                {gameState.isComplete ? (
                  <div className="game-complete-bar" role="status" aria-live="polite">
                    <span className="complete-message">
                      {gaveUp ? 'Better luck next time!' : 'Congratulations!'}
                    </span>
                    <PhraseDisplay 
                      words={gameState.words} 
                      onGuess={handleGuess} 
                      onClue={handleClue} 
                    />
                    <span className="final-score">Score: {gameState.score}</span>
                    <button className="new-game-btn" onClick={handleNewGame}>
                      New Game
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
              </div>
            </>
          ) : (
            <div className="phrase-bar-empty">
              <button className="new-game-btn" onClick={handleNewGame}>
                Start Game
              </button>
              {error && <p className="error-text">{error}</p>}
            </div>
          )}
        </div>
      </section>

      {/* Clue Panel - primary content area */}
      <section className="clue-area">
        <CluePanel 
          tabs={clueTabs}
          activeTabId={activeTabId}
          onTabSelect={setActiveTabId}
          onTabClose={handleTabClose}
          words={gameState?.words ?? []}
        />
      </section>

      {/* Footer */}
      <footer className="game-footer">
        <span className="footer-version">LinkittyDo v{APP_VERSION}</span>
        <span className="footer-status">{getStatusText()}</span>
        <span className="footer-shortcuts">
          <button
            className="mute-toggle"
            onClick={() => { const next = !soundMuted; setSoundMuted(next); setMuted(next); }}
            title={soundMuted ? 'Unmute sound effects' : 'Mute sound effects'}
            aria-label={soundMuted ? 'Unmute sound effects' : 'Mute sound effects'}
          >
            {soundMuted ? 'Sound Off' : 'Sound On'}
          </button>
          <kbd>G</kbd> Give up &middot; <kbd>N</kbd> New game
        </span>
      </footer>

      <UserModal
        isOpen={showUserModal}
        onClose={() => {
          setShowUserModal(false);
          clearError();
        }}
        onRegister={async (name, email, password) => {
          const success = await registerUser({ name, email, password });
          if (success) {
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
            } catch {
              handleNewGame();
            }
          }
          return success;
        }}
        onLogin={async (email, password) => {
          const success = await loginUser({ email, password });
          if (success) {
            handleNewGame();
          }
          return success;
        }}
        onCheckName={checkNameAvailability}
        onCheckEmail={checkEmailAvailability}
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
        onSignOut={handleSignOut}
        onSwitchUser={handleSwitchUser}
        onUpdateDifficulty={updateDifficulty}
        onCreateProfile={() => setShowUserModal(true)}
        loading={userLoading}
      />
    </div>
  );
};
