import { useEffect, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
} from '@mui/material';
import { apiFetch } from '../../api/client';
import { FeatureRoute } from '../../auth/FeatureRoute';
import { useAuth } from '../../auth/AuthContext';
import type { AdminTopic } from '../../types';

export function AdminTopicsPage() {
  const { hasFeature } = useAuth();
  const canWrite = hasFeature('ADMIN_TOPICS', true);
  const [topics, setTopics] = useState<AdminTopic[]>([]);
  const [error, setError] = useState('');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<AdminTopic | null>(null);
  const [displayText, setDisplayText] = useState('');
  const [saving, setSaving] = useState(false);

  const load = () => {
    apiFetch<AdminTopic[]>('/api/admin/topics')
      .then(setTopics)
      .catch((e) => setError(e.message));
  };

  useEffect(() => {
    load();
  }, []);

  const openCreate = () => {
    setEditing(null);
    setDisplayText('');
    setError('');
    setDialogOpen(true);
  };

  const openEdit = (topic: AdminTopic) => {
    setEditing(topic);
    setDisplayText(topic.displayText);
    setError('');
    setDialogOpen(true);
  };

  const closeDialog = () => {
    if (saving) return;
    setDialogOpen(false);
  };

  const save = async () => {
    setSaving(true);
    setError('');
    try {
      if (editing) {
        await apiFetch(`/api/admin/topics/${editing.topicId}`, {
          method: 'PUT',
          body: JSON.stringify({ displayText }),
        });
      } else {
        await apiFetch('/api/admin/topics', {
          method: 'POST',
          body: JSON.stringify({ displayText }),
        });
      }
      setDialogOpen(false);
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed');
    } finally {
      setSaving(false);
    }
  };

  const remove = async (topic: AdminTopic) => {
    if (!window.confirm(`Delete topic "${topic.displayText}"?`)) return;
    setError('');
    try {
      await apiFetch(`/api/admin/topics/${topic.topicId}`, { method: 'DELETE' });
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete failed');
    }
  };

  return (
    <FeatureRoute code="ADMIN_TOPICS">
      <div>
        <div className="admin-page-header">
          <h4>Topics</h4>
          {canWrite && (
            <Button variant="contained" onClick={openCreate}>
              Add topic
            </Button>
          )}
        </div>
        {error && !dialogOpen && <p className="red-text">{error}</p>}
        <div className="collection settings-list">
          {topics.map((topic) => (
            <div key={topic.topicId} className="collection-item settings-list-item">
              {canWrite && (
                <>
                  <button type="button" className="settings-list-edit btn-flat" onClick={() => openEdit(topic)} aria-label={`Edit ${topic.displayText}`}>
                    <i className="material-icons">edit</i>
                  </button>
                  <button type="button" className="settings-list-edit btn-flat" onClick={() => remove(topic)} aria-label={`Delete ${topic.displayText}`}>
                    <i className="material-icons">delete</i>
                  </button>
                </>
              )}
              <div className="settings-list-expression">
                <span className="settings-list-prefix">{topic.displayText} =</span>
                <span className="settings-list-value">{topic.normalizedKey}</span>
              </div>
            </div>
          ))}
        </div>

        <Dialog open={dialogOpen} onClose={closeDialog} fullWidth maxWidth="sm">
          <DialogTitle>{editing ? 'Edit topic' : 'Add topic'}</DialogTitle>
          <DialogContent>
            {error && <p className="red-text">{error}</p>}
            <TextField autoFocus fullWidth margin="dense" label="Display text" value={displayText} onChange={(e) => setDisplayText(e.target.value)} required />
          </DialogContent>
          <DialogActions>
            <Button onClick={closeDialog} disabled={saving}>Cancel</Button>
            <Button variant="contained" onClick={save} disabled={saving || !displayText.trim()}>
              {saving ? 'Saving…' : 'Save'}
            </Button>
          </DialogActions>
        </Dialog>
      </div>
    </FeatureRoute>
  );
}
