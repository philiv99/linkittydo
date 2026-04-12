import { Routes, Route, Navigate } from 'react-router-dom'
import { useState } from 'react'
import { NavHeader } from './components/NavHeader'
import { GameBoard } from './components/GameBoard'
import { UserModal } from './components/UserModal'
import { UserManageModal } from './components/UserManageModal'
import { GameHistoryPage } from './pages/GameHistoryPage'
import { LeaderboardPage } from './pages/LeaderboardPage'
import { TutorialPage } from './pages/TutorialPage'
import { ProfilePage } from './pages/ProfilePage'
import { DailyChallengePage } from './pages/DailyChallengePage'
import { AdminGuard } from './pages/admin/AdminGuard'
import { AdminLayout } from './pages/admin/AdminLayout'
import { AdminDashboard } from './pages/admin/AdminDashboard'
import { AdminUsers } from './pages/admin/AdminUsers'
import { AdminGames } from './pages/admin/AdminGames'
import { AdminConfig } from './pages/admin/AdminConfig'
import { AdminDataExplorer } from './pages/admin/AdminDataExplorer'
import { AdminPhrases } from './pages/admin/AdminPhrases'
import { AdminAuditLog } from './pages/admin/AdminAuditLog'
import { useUser } from './hooks/useUser'
import './App.css'

function App() {
  const {
    user,
    isGuest,
    isAdmin,
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
        isAdmin={isAdmin}
        lifetimePoints={user.lifetimePoints}
        onUserClick={handleUserClick}
      />
      <Routes>
        <Route path="/" element={<Navigate to="/play" replace />} />
        <Route path="/play" element={<GameBoard />} />
        <Route path="/daily" element={<DailyChallengePage />} />
        <Route path="/tutorial" element={<TutorialPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/history" element={<GameHistoryPage />} />
        <Route path="/leaderboard" element={<LeaderboardPage />} />
        <Route path="/admin" element={<AdminGuard><AdminLayout /></AdminGuard>}>
          <Route index element={<Navigate to="dashboard" replace />} />
          <Route path="dashboard" element={<AdminDashboard />} />
          <Route path="users" element={<AdminUsers />} />
          <Route path="games" element={<AdminGames />} />
          <Route path="phrases" element={<AdminPhrases />} />
          <Route path="config" element={<AdminConfig />} />
          <Route path="data" element={<AdminDataExplorer />} />
          <Route path="audit" element={<AdminAuditLog />} />
        </Route>
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
