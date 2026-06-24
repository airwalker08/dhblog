import { useEffect, useState } from 'react';
import { apiFetch } from '../api/client';
import type { BlogEntry } from '../types';
import { useAuth } from '../auth/AuthContext';

export function FeedPage() {
  const { user } = useAuth();
  const [entries, setEntries] = useState<BlogEntry[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    apiFetch<BlogEntry[]>('/api/feed')
      .then(setEntries)
      .catch((e) => setError(e.message));
  }, []);

  return (
    <div>
      <h4>Feed</h4>
      <p className="grey-text">Posts from users you follow, {user?.username}.</p>
      {error && <p className="red-text">{error}</p>}
      {entries.length === 0 && !error && <p>No posts in your feed yet. Follow users to see their blog entries.</p>}
      {entries.map((entry) => (
        <div key={entry.entryId} className="card blog-entry-card">
          <div className="card-content">
            <span className="card-title">{entry.title || 'Untitled'}</span>
            <p className="grey-text feed-entry-author">{entry.username}</p>
            <p>{entry.text}</p>
            {entry.topics.length > 0 && (
              <div>
                {entry.topics.map((t) => (
                  <span key={t} className="chip">{t}</span>
                ))}
              </div>
            )}
            {entry.images.map((img) => (
              <img key={img.imageId} src={img.url} alt="" className="blog-entry-image" />
            ))}
            <p className="grey-text">{new Date(entry.createdAt).toLocaleString()}</p>
          </div>
        </div>
      ))}
    </div>
  );
}
