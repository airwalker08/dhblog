import { useEffect, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  TextField,
} from '@mui/material';
import { apiFetch } from '../../api/client';
import { FeatureRoute } from '../../auth/FeatureRoute';
import { useAuth } from '../../auth/AuthContext';
import type { AdminRole, AdminUser } from '../../types';

const emptyForm = {
  username: '',
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  roleId: '',
  locale: 'en-US',
  timeZone: 'UTC',
  language: 'en',
};

export function AdminUsersPage() {
  const { hasFeature } = useAuth();
  const canWrite = hasFeature('ADMIN_USERS', true);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [roles, setRoles] = useState<AdminRole[]>([]);
  const [error, setError] = useState('');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<AdminUser | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);

  const load = () => {
    Promise.all([
      apiFetch<AdminUser[]>('/api/admin/users'),
      apiFetch<AdminRole[]>('/api/admin/roles'),
    ])
      .then(([u, r]) => {
        setUsers(u);
        setRoles(r);
      })
      .catch((e) => setError(e.message));
  };

  useEffect(() => {
    load();
  }, []);

  const openCreate = () => {
    setEditing(null);
    setForm({ ...emptyForm, roleId: roles[0]?.roleId ?? '' });
    setError('');
    setDialogOpen(true);
  };

  const openEdit = (user: AdminUser) => {
    setEditing(user);
    setForm({
      username: user.username,
      email: user.email,
      password: '',
      firstName: user.firstName,
      lastName: user.lastName,
      roleId: user.roleId,
      locale: user.locale,
      timeZone: user.timeZone,
      language: user.language,
    });
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
        await apiFetch(`/api/admin/users/${editing.userId}`, {
          method: 'PUT',
          body: JSON.stringify({
            ...form,
            password: form.password || null,
          }),
        });
      } else {
        await apiFetch('/api/admin/users', {
          method: 'POST',
          body: JSON.stringify(form),
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

  const remove = async (user: AdminUser) => {
    if (!window.confirm(`Delete user "${user.username}"?`)) return;
    setError('');
    try {
      await apiFetch(`/api/admin/users/${user.userId}`, { method: 'DELETE' });
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete failed');
    }
  };

  return (
    <FeatureRoute code="ADMIN_USERS">
      <div>
        <div className="admin-page-header">
          <h4>Users</h4>
          {canWrite && (
            <Button variant="contained" onClick={openCreate}>
              Add user
            </Button>
          )}
        </div>
        {error && !dialogOpen && <p className="red-text">{error}</p>}
        <div className="collection settings-list">
          {users.map((user) => (
            <div key={user.userId} className="collection-item settings-list-item">
              {canWrite && (
                <>
                  <button
                    type="button"
                    className="settings-list-edit btn-flat"
                    onClick={() => openEdit(user)}
                    aria-label={`Edit ${user.username}`}
                  >
                    <i className="material-icons">edit</i>
                  </button>
                  <button
                    type="button"
                    className="settings-list-edit btn-flat"
                    onClick={() => remove(user)}
                    aria-label={`Delete ${user.username}`}
                  >
                    <i className="material-icons">delete</i>
                  </button>
                </>
              )}
              <div className="settings-list-expression">
                <span className="settings-list-prefix">{user.username} =</span>
                <span className="settings-list-value">
                  {user.email} · {user.roleName} · {user.firstName} {user.lastName}
                </span>
              </div>
            </div>
          ))}
        </div>

        <Dialog open={dialogOpen} onClose={closeDialog} fullWidth maxWidth="sm">
          <DialogTitle>{editing ? 'Edit user' : 'Add user'}</DialogTitle>
          <DialogContent>
            {error && <p className="red-text">{error}</p>}
            <TextField fullWidth margin="dense" label="Username" value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} required />
            <TextField fullWidth margin="dense" label="Email" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
            <TextField fullWidth margin="dense" label={editing ? 'Password (leave blank to keep)' : 'Password'} type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} required={!editing} />
            <TextField fullWidth margin="dense" label="First name" value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} />
            <TextField fullWidth margin="dense" label="Last name" value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} />
            <TextField select fullWidth margin="dense" label="Role" value={form.roleId} onChange={(e) => setForm({ ...form, roleId: e.target.value })} required>
              {roles.map((r) => (
                <MenuItem key={r.roleId} value={r.roleId}>{r.name}</MenuItem>
              ))}
            </TextField>
            <TextField fullWidth margin="dense" label="Locale" value={form.locale} onChange={(e) => setForm({ ...form, locale: e.target.value })} />
            <TextField fullWidth margin="dense" label="Time zone" value={form.timeZone} onChange={(e) => setForm({ ...form, timeZone: e.target.value })} />
            <TextField fullWidth margin="dense" label="Language" value={form.language} onChange={(e) => setForm({ ...form, language: e.target.value })} />
          </DialogContent>
          <DialogActions>
            <Button onClick={closeDialog} disabled={saving}>Cancel</Button>
            <Button variant="contained" onClick={save} disabled={saving || !form.username.trim() || !form.email.trim() || !form.roleId || (!editing && !form.password)}>
              {saving ? 'Saving…' : 'Save'}
            </Button>
          </DialogActions>
        </Dialog>
      </div>
    </FeatureRoute>
  );
}
