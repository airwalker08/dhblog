import { useEffect, useState } from 'react';
import { TextField, Button } from '@mui/material';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';

export function ProfilePage() {
  const { user, refresh } = useAuth();
  const [following, setFollowing] = useState<string[]>([]);
  const [followUsername, setFollowUsername] = useState('');
  const [message, setMessage] = useState('');

  useEffect(() => {
    apiFetch<string[]>('/api/follows').then(setFollowing).catch(() => setFollowing([]));
  }, []);

  const follow = async () => {
    setMessage('');
    try {
      await apiFetch(`/api/follows/${encodeURIComponent(followUsername)}`, { method: 'POST' });
      setFollowing(await apiFetch<string[]>('/api/follows'));
      setFollowUsername('');
      setMessage(`Now following ${followUsername}`);
    } catch (e) {
      setMessage(e instanceof Error ? e.message : 'Failed');
    }
  };

  const unfollow = async (username: string) => {
    await apiFetch(`/api/follows/${encodeURIComponent(username)}`, { method: 'DELETE' });
    setFollowing(await apiFetch<string[]>('/api/follows'));
  };

  if (!user) return null;

  return (
    <div>
      <h4>Profile</h4>
      <div className="card">
        <div className="card-content">
          <p><strong>Username:</strong> {user.username}</p>
          <p><strong>Email:</strong> {user.email}</p>
          <p><strong>Name:</strong> {user.firstName} {user.lastName}</p>
          <p><strong>Role:</strong> {user.roleName}</p>
          <p><strong>Locale:</strong> {user.locale} | <strong>Time zone:</strong> {user.timeZone}</p>
        </div>
      </div>

      <h5>Following</h5>
      <div className="flex-row">
        <TextField
          size="small"
          label="Username to follow"
          value={followUsername}
          onChange={(e) => setFollowUsername(e.target.value)}
        />
        <Button variant="contained" onClick={follow} disabled={!followUsername.trim()}>
          Follow
        </Button>
      </div>
      {message && <p>{message}</p>}
      <ul className="collection">
        {following.map((u) => (
          <li key={u} className="collection-item">
            {u}
            <Button size="small" className="btn-spaced-left" onClick={() => unfollow(u)}>Unfollow</Button>
          </li>
        ))}
      </ul>
      <Button className="btn-spaced-top" onClick={() => refresh()}>Refresh profile</Button>
    </div>
  );
}
