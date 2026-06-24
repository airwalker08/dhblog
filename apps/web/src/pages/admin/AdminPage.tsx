import { Link, Navigate } from 'react-router-dom';
import { useMemo } from 'react';
import { useAuth } from '../../auth/AuthContext';
import { buildNavTree, findNavNode } from '../../nav/buildNavTree';
import { canAccessNavGroup } from '../../nav/navAccess';

export function AdminPage() {
  const { user } = useAuth();
  const adminNode = useMemo(() => {
    const tree = buildNavTree(user?.features ?? []);
    return findNavNode(tree, 'ADMIN');
  }, [user?.features]);

  const children = adminNode?.children.filter((c) => c.canRead && c.navPath) ?? [];

  if (!canAccessNavGroup(user?.features ?? [], 'ADMIN')) {
    return <Navigate to="/feed" replace />;
  }

  return (
    <div>
      <h4>Admin</h4>
      <p className="grey-text">Manage application data and configuration.</p>
      <div className="admin-section-grid">
        {children.map((item) => (
          <Link key={item.code} to={item.navPath} className="card admin-section-card hoverable">
            <div className="card-content center-align">
              <span className="card-title">{item.name}</span>
            </div>
          </Link>
        ))}
      </div>
      {children.length === 0 && (
        <p className="grey-text">No admin sections are available for your account.</p>
      )}
    </div>
  );
}
