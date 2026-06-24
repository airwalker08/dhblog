import { Outlet } from 'react-router-dom';

export function PreAuthLayout() {
  return (
    <div className="pre-auth-layout">
      <main className="container">
        <Outlet />
      </main>
      <footer className="page-footer blue darken-2">
        <div className="container">
          <div className="row">
            <div className="col s12">
              <span className="white-text">dhblog — personal development sandbox</span>
            </div>
          </div>
        </div>
        <div className="footer-copyright">
          <div className="container center-align white-text">&copy; {new Date().getFullYear()} dhblog</div>
        </div>
      </footer>
    </div>
  );
}
