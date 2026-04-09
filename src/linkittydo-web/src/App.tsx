import { Routes, Route, Navigate } from 'react-router-dom'
import { useState } from 'react'
import { NavHeader } from './components/NavHeader'
import { GameBoard } from './components/GameBoard'
import { UserModal } from './components/UserModal'
import { UserManageModal } from './components/UserManageModal'
import { GameHistoryPage } from './pages/GameHistoryPage'
import { LeaderboardPage } from './pages/LeaderboardPage'
import { useUser } from './hooks/useUser'
import './App.css'

function App() {
  const {
    user,
    isGuest,
    allUsers,
    loading: userLoading,
    error: userError,
    registerUser,
    loginUser,
    switchUser,
    checkNameAvailability,
    checkEmailAvailability,
    clearError,
    signOut,
  } = useUser();

  const [showUserModal, setShowUserModal] = useState(false);
  const [showManageModal, setShowManageModal] = useState(false);

  const handleUserClick = () => {
    if (isGuest) {
      setShowUserModal(true);
    } else {
      setShowManageModal(true);
    }
  };

  return (
    <>
      <NavHeader
        userName={user.name}
        isGuest={isGuest}
        lifetimePoints={user.lifetimePoints}
        onUserClick={handleUserClick}
      />
      <Routes>
        <Route path="/" element={<Navigate to="/play" replace />} />
        <Route path="/play" element={<GameBoard />} />
        <Route path="/history" element={<GameHistoryPage />} />
        <Route path="/leaderboard" element={<LeaderboardPage />} />
      </Routes>

      <UserModal
        isOpen={showUserModal}
        onClose={() => {
          setShowUserModal(false);
          clearError();
        }}
        onRegister={async (name, email, password) => {
          const success = await registerUser({ name, email, password });
          if (success) setShowUserModal(false);
          return success;
        }}
        onLogin={async (email, password) => {
          const success = await loginUser({ email, password });
          if (success) setShowUserModal(false);
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
        onSignOut={() => {
          signOut();
          setShowManageModal(false);
        }}
        onSwitchUser={switchUser}
        onUpdateDifficulty={async () => true}
        onCreateProfile={() => {
          setShowManageModal(false);
          setShowUserModal(true);
        }}
        loading={userLoading}
      />
    </>
  )
}

export default App
