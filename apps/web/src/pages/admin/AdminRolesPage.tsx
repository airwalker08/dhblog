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
import type { AdminRole } from '../../types';

export function AdminRolesPage() {
  const { hasFeature } = useAuth();
  const canWrite = hasFeature('ADMIN_ROLES', true);
  const [roles, setRoles] = useState<AdminRole[]>([]);
  const [error, setError] = useState('');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<AdminRole | null>(null);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [saving, setSaving] = useState(false);

  const load = () => {
    apiFetch<AdminRole[]>('/api/admin/roles')
      .then(setRoles)
      .catch((e) => setError(e.message));
  };

  useEffect(() => {
    load();
  }, []);

  const openCreate = () => {
    setEditing(null);
    setName('');
    setDescription('');
    setError('');
    setDialogOpen(true);
  };

  const openEdit = (role: AdminRole) => {
    setEditing(role);
    setName(role.name);
    setDescription(role.description);
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
        await apiFetch(`/api/admin/roles/${editing.roleId}`, {
          method: 'PUT',
          body: JSON.stringify({ name, description }),
        });
      } else {
        await apiFetch('/api/admin/roles', {
          method: 'POST',
          body: JSON.stringify({ name, description }),
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

  const remove = async (role: AdminRole) => {
    if (!window.confirm(`Delete role "${role.name}"?`)) return;
    setError('');
    try {
      await apiFetch(`/api/admin/roles/${role.roleId}`, { method: 'DELETE' });
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete failed');
    }
  };

  return (
    <FeatureRoute code="ADMIN_ROLES">
      <div>
        <div className="admin-page-header">
          <h4>Roles</h4>
          {canWrite && (
            <Button variant="contained" onClick={openCreate}>
              Add role
            </Button>
          )}
        </div>
        {error && !dialogOpen && <p className="red-text">{error}</p>}
        <div className="collection settings-list">
          {roles.map((role) => (
            <div key={role.roleId} className="collection-item settings-list-item">
              {canWrite && (
                <>
                  <button type="button" className="settings-list-edit btn-flat" onClick={() => openEdit(role)} aria-label={`Edit ${role.name}`}>
                    <i className="material-icons">edit</i>
                  </button>
                  <button type="button" className="settings-list-edit btn-flat" onClick={() => remove(role)} aria-label={`Delete ${role.name}`}>
                    <i className="material-icons">delete</i>
                  </button>
                </>
              )}
              <div className="settings-list-expression">
                <span className="settings-list-prefix">{role.name} =</span>
                <span className="settings-list-value">{role.description || '—'}</span>
              </div>
            </div>
          ))}
        </div>

        <Dialog open={dialogOpen} onClose={closeDialog} fullWidth maxWidth="sm">
          <DialogTitle>{editing ? 'Edit role' : 'Add role'}</DialogTitle>
          <DialogContent>
            {error && <p className="red-text">{error}</p>}
            <TextField autoFocus fullWidth margin="dense" label="Name" value={name} onChange={(e) => setName(e.target.value)} required />
            <TextField fullWidth margin="dense" label="Description" value={description} onChange={(e) => setDescription(e.target.value)} multiline minRows={2} />
          </DialogContent>
          <DialogActions>
            <Button onClick={closeDialog} disabled={saving}>Cancel</Button>
            <Button variant="contained" onClick={save} disabled={saving || !name.trim()}>
              {saving ? 'Saving…' : 'Save'}
            </Button>
          </DialogActions>
        </Dialog>
      </div>
    </FeatureRoute>
  );
}
