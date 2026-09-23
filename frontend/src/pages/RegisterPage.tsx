import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { Field } from '../components/Common';
import { useAuth } from '../context/AuthContext';
import { errorMessage, fieldError } from '../utils/format';

export default function RegisterPage() {
  const { user, register } = useAuth();
  const navigate = useNavigate();

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to={user.isAdmin ? '/admin' : '/dashboard'} replace />;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await register(fullName, email, password);
      navigate('/courses', { replace: true });
    } catch (err) {
      setError(err);
      setBusy(false);
    }
  }

  const hasFieldErrors = ['fullName', 'email', 'password'].some((key) => fieldError(error, key));

  return (
    <div className="auth">
      <form className="panel auth__form" onSubmit={handleSubmit} noValidate>
        <h1>Create your account</h1>
        <p className="muted">Enroll in courses and track your progress.</p>

        {error !== null && !hasFieldErrors && (
          <div className="notice notice--error" role="alert">
            {errorMessage(error)}
          </div>
        )}

        <Field label="Full name" htmlFor="fullName" error={fieldError(error, 'fullName')}>
          <input id="fullName" autoComplete="name" value={fullName} onChange={(e) => setFullName(e.target.value)} required />
        </Field>
        <Field label="Email" htmlFor="email" error={fieldError(error, 'email')}>
          <input id="email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </Field>
        <Field
          label="Password"
          htmlFor="password"
          error={fieldError(error, 'password')}
          hint="At least 8 characters with an uppercase letter, a lowercase letter and a number."
        >
          <input
            id="password"
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </Field>

        <button type="submit" className="btn btn--primary btn--block" disabled={busy}>
          {busy ? 'Creating account...' : 'Create account'}
        </button>

        <p className="auth__switch">
          Already registered? <Link to="/login">Log in</Link>
        </p>
      </form>
    </div>
  );
}
