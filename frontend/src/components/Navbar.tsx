import { useState } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { BrandMark } from './Common';

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [busy, setBusy] = useState(false);

  async function handleLogout() {
    setBusy(true);
    await logout();
    setBusy(false);
    navigate('/courses');
  }

  return (
    <header className="topbar">
      <div className="container topbar__inner">
        <Link to="/courses" className="brand">
          <BrandMark />
          EduTrack
        </Link>

        <nav className="topbar__nav" aria-label="Main">
          <NavLink to="/courses">Courses</NavLink>
          {user && !user.isAdmin && <NavLink to="/dashboard">My learning</NavLink>}
          {user?.isAdmin && <NavLink to="/admin">Admin</NavLink>}
        </nav>

        <div className="topbar__user">
          {user ? (
            <>
              <span className="topbar__name">{user.fullName}</span>
              <button type="button" className="btn btn--ghost btn--sm" onClick={handleLogout} disabled={busy}>
                Log out
              </button>
            </>
          ) : (
            <>
              <Link className="btn btn--ghost btn--sm" to="/login">
                Log in
              </Link>
              <Link className="btn btn--primary btn--sm" to="/register">
                Create account
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
