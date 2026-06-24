import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Alert, Button, TextField } from '@mui/material';
import { apiFetch } from '../api/client';

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [token, setToken] = useState(searchParams.get('token') ?? '');
  const [password, setPassword] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await apiFetch('/api/auth/reset-password', {
        method: 'POST',
        body: JSON.stringify({ token, newPassword: password }),
      });
      setMessage('Password updated. You can sign in now.');
      setTimeout(() => navigate('/login'), 1500);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Reset failed');
    }
  };

  return (
    <div className="login-card card">
      <div className="card-content">
        <span className="card-title">Reset password</span>
        {error && (
          <div className="alert-stack">
            <Alert severity="error">{error}</Alert>
          </div>
        )}
        {message && (
          <div className="alert-stack">
            <Alert severity="success">{message}</Alert>
          </div>
        )}
        <form onSubmit={handleSubmit}>
          <TextField
            fullWidth
            margin="normal"
            label="Reset token"
            value={token}
            onChange={(e) => setToken(e.target.value)}
            required
          />
          <TextField
            fullWidth
            margin="normal"
            label="New password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
          <div className="form-actions">
            <Link to="/login">Back to login</Link>
            <Button type="submit" variant="contained">Update password</Button>
          </div>
        </form>
      </div>
    </div>
  );
}
