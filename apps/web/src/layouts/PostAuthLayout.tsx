import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link, Outlet, useNavigate } from 'react-router-dom';
import { SidebarNav } from '../components/SidebarNav';
import { buildNavTree } from '../nav/buildNavTree';
import { useAuth } from '../auth/AuthContext';

const SIDEBAR_MIN_WIDTH = 180;
const SIDEBAR_MAX_WIDTH = 400;
const SIDEBAR_DEFAULT_WIDTH = 240;
const SIDEBAR_COLLAPSED_WIDTH = 48;

function readStoredWidth(): number {
  const stored = localStorage.getItem('dhblog-sidebar-width');
  const parsed = stored ? Number.parseInt(stored, 10) : SIDEBAR_DEFAULT_WIDTH;
  if (Number.isNaN(parsed)) return SIDEBAR_DEFAULT_WIDTH;
  return Math.min(SIDEBAR_MAX_WIDTH, Math.max(SIDEBAR_MIN_WIDTH, parsed));
}

export function PostAuthLayout() {
  const { user, logout, hasFeature } = useAuth();
  const navigate = useNavigate();
  const [collapsed, setCollapsed] = useState(
    () => localStorage.getItem('dhblog-sidebar-collapsed') === 'true',
  );
  const [sidebarWidth, setSidebarWidth] = useState(readStoredWidth);
  const resizing = useRef(false);

  const navTree = useMemo(
    () => buildNavTree(user?.features ?? []),
    [user?.features],
  );
  const effectiveSidebarWidth = collapsed ? SIDEBAR_COLLAPSED_WIDTH : sidebarWidth;

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const toggleSidebar = () => {
    setCollapsed((prev) => {
      const next = !prev;
      localStorage.setItem('dhblog-sidebar-collapsed', String(next));
      return next;
    });
  };

  const startResize = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    resizing.current = true;
    document.body.classList.add('dhblog-resizing');
  }, []);

  useEffect(() => {
    localStorage.setItem('dhblog-sidebar-width', String(sidebarWidth));
  }, [sidebarWidth]);

  useEffect(() => {
    const onMouseMove = (e: MouseEvent) => {
      if (!resizing.current) return;
      const next = Math.min(
        SIDEBAR_MAX_WIDTH,
        Math.max(SIDEBAR_MIN_WIDTH, e.clientX),
      );
      setSidebarWidth(next);
    };

    const onMouseUp = () => {
      if (!resizing.current) return;
      resizing.current = false;
      document.body.classList.remove('dhblog-resizing');
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
    return () => {
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
      document.body.classList.remove('dhblog-resizing');
    };
  }, []);

  useEffect(() => {
    const trigger = document.querySelector('.dhblog-user-dropdown-trigger');
    if (!trigger || !user) return;

    // @ts-expect-error Materialize global
    if (!window.M) return;

    // @ts-expect-error Materialize global
    const existing = window.M.Dropdown.getInstance(trigger);
    if (existing) existing.destroy();

    // @ts-expect-error Materialize global
    window.M.Dropdown.init(trigger, {
      coverTrigger: false,
      constrainWidth: false,
      alignment: 'right',
    });
  }, [user]);

  return (
    <div
      className="post-auth-layout"
      style={{ '--dhblog-sidebar-width': `${effectiveSidebarWidth}px` } as React.CSSProperties}
    >
      <header className="dhblog-header-fixed">
        <nav className="dhblog-header">
          <div className="nav-wrapper dhblog-nav-wrapper">
            <Link to="/feed" className="brand-logo dhblog-logo">
              dhblog
            </Link>
            <ul className="right">
              <li>
                <a
                  className="dropdown-trigger dhblog-user-dropdown-trigger white-text"
                  href="#!"
                  data-target="user-dropdown"
                >
                  {user?.username}
                  <i className="material-icons right">arrow_drop_down</i>
                </a>
              </li>
            </ul>
            <ul id="user-dropdown" className="dropdown-content">
              {hasFeature('PROFILE') && (
                <li>
                  <Link to="/profile">Profile</Link>
                </li>
              )}
              <li>
                <a
                  href="#!"
                  onClick={(e) => {
                    e.preventDefault();
                    handleLogout();
                  }}
                >
                  Log out
                </a>
              </li>
            </ul>
          </div>
        </nav>
      </header>

      <aside
        className={`dhblog-sidebar${collapsed ? ' dhblog-sidebar--collapsed' : ''}`}
        style={{ width: effectiveSidebarWidth }}
      >
        <button
          type="button"
          className="dhblog-sidebar-toggle"
          onClick={toggleSidebar}
          aria-label={collapsed ? 'Expand navigation' : 'Collapse navigation'}
          aria-expanded={!collapsed}
        >
          {collapsed ? '>' : '<'}
        </button>
        {!collapsed && (
          <>
            <div className="dhblog-sidebar-nav" role="navigation">
              <SidebarNav nodes={navTree} />
            </div>
            <div
              className="dhblog-sidebar-resize"
              onMouseDown={startResize}
              role="separator"
              aria-orientation="vertical"
              aria-label="Resize navigation"
            />
          </>
        )}
      </aside>

      <div className="dhblog-main-with-sidenav">
        <main>
          <Outlet />
        </main>
      </div>

      <footer className="page-footer blue darken-2">
        <div className="footer-copyright">
          <div className="container center-align white-text">&copy; {new Date().getFullYear()} dhblog</div>
        </div>
      </footer>
    </div>
  );
}
