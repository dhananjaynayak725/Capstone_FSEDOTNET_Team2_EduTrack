import { Link, Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { EmptyState, Spinner } from './Common';

/** Layout route: renders child routes only for signed-in users (and admins, when `admin` is set). */
export default function RequireAuth({ admin = false }: { admin?: boolean }) {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading) return <Spinner label="Checking your session" />;

  if (!user) {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />;
  }

  if (admin && !user.isAdmin) {
    return (
      <EmptyState
        title="This area is for admins"
        action={
          <Link className="btn btn--primary" to="/courses">
            Browse courses
          </Link>
        }
      >
        Your account does not have admin access.
      </EmptyState>
    );
  }

  return <Outlet />;
}
