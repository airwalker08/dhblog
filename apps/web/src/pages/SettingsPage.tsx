import { useEffect, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
} from '@mui/material';
import { apiFetch } from '../api/client';

export function SettingsPage() {
  const [settings, setSettings] = useState<Record<string, string>>({});
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [editValue, setEditValue] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    apiFetch<Record<string, string>>('/api/settings').then(setSettings);
  }, []);

  const openEdit = (key: string, value: string) => {
    setError('');
    setEditingKey(key);
    setEditValue(value);
  };

  const closeEdit = () => {
    if (saving) return;
    setEditingKey(null);
    setEditValue('');
  };

  const save = async () => {
    if (!editingKey) return;
    setSaving(true);
    setError('');
    try {
      await apiFetch(`/api/settings/${editingKey}`, {
        method: 'PUT',
        body: JSON.stringify({ value: editValue }),
      });
      setSettings((s) => ({ ...s, [editingKey]: editValue }));
      setMessage(`Saved ${editingKey}`);
      setEditingKey(null);
      setEditValue('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div>
      <h4>Settings</h4>
      <p className="grey-text">
        Configurable key/value settings (Parameter Store in AWS, local env in development).
      </p>
      {message && <p className="green-text">{message}</p>}

      <div className="collection settings-list">
        {Object.entries(settings).map(([key, value]) => (
          <div key={key} className="collection-item settings-list-item">
            <button
              type="button"
              className="settings-list-edit btn-flat"
              onClick={() => openEdit(key, value)}
              aria-label={`Edit ${key}`}
            >
              <i className="material-icons">edit</i>
            </button>
            <div className="settings-list-expression">
              <span className="settings-list-prefix">{key} =</span>
              <span className="settings-list-value">{value}</span>
            </div>
          </div>
        ))}
      </div>

      <Dialog open={editingKey !== null} onClose={closeEdit} fullWidth maxWidth="sm">
        <DialogTitle>Edit setting</DialogTitle>
        <DialogContent>
          <p className="grey-text settings-dialog-key">
            {editingKey}
          </p>
          {error && <p className="red-text">{error}</p>}
          <TextField
            autoFocus
            fullWidth
            margin="dense"
            label="Value"
            value={editValue}
            onChange={(e) => setEditValue(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={closeEdit} disabled={saving}>
            Cancel
          </Button>
          <Button variant="contained" onClick={save} disabled={saving}>
            {saving ? 'Saving…' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
    </div>
  );
}
