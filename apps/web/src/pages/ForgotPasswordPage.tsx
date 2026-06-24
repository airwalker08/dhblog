import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Alert, Button, TextField } from '@mui/material';
import { apiFetch } from '../api/client';

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [devToken, setDevToken] = useState<string | null>(null);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      const result = await apiFetch<{ devToken?: string; message: string }>('/api/auth/forgot-password', {
        method: 'POST',
        body: JSON.stringify({ email }),
      });
      setMessage(result.message);
      setDevToken(result.devToken ?? null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Request failed');
    }
  };

  return (
    <div className="login-card card">
      <div className="card-content">
        <span className="card-title">Forgot password</span>
        {error && (
          <div className="alert-stack">
            <Alert severity="error">{error}</Alert>
          </div>
        )}
        {message && (
          <div className="alert-stack">
            <Alert severity="info">{message}</Alert>
          </div>
        )}
        {devToken && (
          <div className="alert-stack">
            <Alert severity="warning">
              Dev token: <Link to={`/reset-password?token=${devToken}`}>{devToken}</Link>
            </Alert>
          </div>
        )}
        <form onSubmit={handleSubmit}>
          <TextField
            fullWidth
            margin="normal"
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <div className="form-actions">
            <Link to="/login">Back to login</Link>
            <Button type="submit" variant="contained">Send reset link</Button>
          </div>
        </form>
      </div>
    </div>
  );
}
