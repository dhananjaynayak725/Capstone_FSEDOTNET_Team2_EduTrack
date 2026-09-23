import { useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Field } from '../components/Common';
import { useAuth } from '../context/AuthContext';
import { errorMessage, fieldError } from '../utils/format';

const DEMO_ACCOUNTS = [
  { label: 'Student', email: 'student@example.com', password: 'Student@123' },
  { label: 'Admin', email: 'admin@example.com', password: 'Admin@123' },
];

export default function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from;

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to={from ?? (user.isAdmin ? '/admin' : '/dashboard')} replace />;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const signedIn = await login(email, password);
      navigate(from ?? (signedIn.isAdmin ? '/admin' : '/dashboard'), { replace: true });
    } catch (err) {
      setError(err);
      setBusy(false);
    }
  }

  return (
    <div className="auth">
      <form className="panel auth__form" onSubmit={handleSubmit} noValidate>
        <h1>Log in</h1>
        <p className="muted">Pick up where you left off.</p>

        {error !== null && (
          <div className="notice notice--error" role="alert">
            {errorMessage(error)}
          </div>
        )}

        <Field label="Email" htmlFor="email" error={fieldError(error, 'email')}>
          <input id="email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </Field>
        <Field label="Password" htmlFor="password" error={fieldError(error, 'password')}>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </Field>

        <button type="submit" className="btn btn--primary btn--block" disabled={busy}>
          {busy ? 'Logging in...' : 'Log in'}
        </button>

        <p className="auth__switch">
          New here? <Link to="/register">Create an account</Link>
        </p>
      </form>

      <aside className="auth__demo">
        <h2>Demo accounts</h2>
        <p className="muted">Seeded on first run. Fill the form with one click.</p>
        <div className="auth__demo-list">
          {DEMO_ACCOUNTS.map((account) => (
            <button
              key={account.label}
              type="button"
              className="btn btn--ghost"
              onClick={() => {
                setEmail(account.email);
                setPassword(account.password);
              }}
            >
              Use {account.label.toLowerCase()} account
            </button>
          ))}
        </div>
      </aside>
    </div>
  );
}
