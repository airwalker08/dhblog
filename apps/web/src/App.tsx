import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { GuestRoute, ProtectedRoute } from './auth/ProtectedRoute';
import { PreAuthLayout } from './layouts/PreAuthLayout';
import { PostAuthLayout } from './layouts/PostAuthLayout';
import { LoginPage } from './pages/LoginPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import { ResetPasswordPage } from './pages/ResetPasswordPage';
import { FeedPage } from './pages/FeedPage';
import { BlogPage } from './pages/BlogPage';
import { ProfilePage } from './pages/ProfilePage';
import { SettingsPage } from './pages/SettingsPage';
import { DiagnosticsPage } from './pages/DiagnosticsPage';
import { AdminPage } from './pages/admin/AdminPage';
import { AdminUsersPage } from './pages/admin/AdminUsersPage';
import { AdminRolesPage } from './pages/admin/AdminRolesPage';
import { AdminTopicsPage } from './pages/admin/AdminTopicsPage';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<GuestRoute />}>
            <Route element={<PreAuthLayout />}>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/forgot-password" element={<ForgotPasswordPage />} />
              <Route path="/reset-password" element={<ResetPasswordPage />} />
            </Route>
          </Route>

          <Route element={<ProtectedRoute />}>
            <Route element={<PostAuthLayout />}>
              <Route path="/feed" element={<FeedPage />} />
              <Route path="/blog" element={<BlogPage />} />
              <Route path="/profile" element={<ProfilePage />} />
              <Route path="/settings" element={<SettingsPage />} />
              <Route path="/diagnostics" element={<DiagnosticsPage />} />
              <Route path="/admin" element={<AdminPage />} />
              <Route path="/admin/users" element={<AdminUsersPage />} />
              <Route path="/admin/roles" element={<AdminRolesPage />} />
              <Route path="/admin/topics" element={<AdminTopicsPage />} />
            </Route>
          </Route>

          <Route path="/" element={<Navigate to="/feed" replace />} />
          <Route path="*" element={<Navigate to="/feed" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
