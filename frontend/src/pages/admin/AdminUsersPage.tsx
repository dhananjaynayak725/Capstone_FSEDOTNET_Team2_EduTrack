import { useState, type FormEvent } from 'react';
import { usersApi } from '../../api/endpoints';
import type { User, UserRole } from '../../api/types';
import { EmptyState, ErrorNotice, Field, PageHeader, Spinner } from '../../components/Common';
import Modal from '../../components/Modal';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import { errorMessage, fieldError, formatDate } from '../../utils/format';
import { useDebounced, useLoad } from '../../utils/hooks';

type RoleFilter = 'all' | UserRole;

const ROLE_TABS: { value: RoleFilter; label: string }[] = [
  { value: 'all', label: 'Everyone' },
  { value: 'student', label: 'Students' },
  { value: 'instructor', label: 'Instructors' },
  { value: 'admin', label: 'Admins' },
];

function roleLabel(user: User): string {
  if (user.isAdmin) return 'Admin';
  if (user.isInstructor) return 'Instructor';
  return 'Student';
}

export default function AdminUsersPage() {
  const { user: me } = useAuth();
  const { notify } = useToast();
  const [role, setRole] = useState<RoleFilter>('all');
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounced(search);

  const users = useLoad(() => usersApi.list({ search: debouncedSearch, role: role === 'all' ? undefined : role }), [debouncedSearch, role]);

  const [editing, setEditing] = useState<User | null>(null);
  const [deleting, setDeleting] = useState<User | null>(null);
  const [deleteBusy, setDeleteBusy] = useState(false);

  async function confirmDelete() {
    if (!deleting) return;
    setDeleteBusy(true);
    try {
      await usersApi.remove(deleting.id);
      notify(`Deleted ${deleting.fullName}.`);
      setDeleting(null);
      users.reload();
    } catch (err) {
      notify(errorMessage(err), 'error');
      setDeleting(null);
    } finally {
      setDeleteBusy(false);
    }
  }

  const items = users.data ?? [];

  return (
    <>
      <PageHeader title="People" intro="Manage students and instructors. Mark someone as an instructor to let them be assigned to courses." />

      <div className="filters">
        <div className="tabs tabs--inline" role="tablist" aria-label="Filter by role">
          {ROLE_TABS.map((tab) => (
            <button
              key={tab.value}
              type="button"
              role="tab"
              aria-selected={role === tab.value}
              className={role === tab.value ? 'tab tab--active' : 'tab'}
              onClick={() => setRole(tab.value)}
            >
              {tab.label}
            </button>
          ))}
        </div>
        <div className="filters__field filters__field--grow">
          <label htmlFor="user-search">Search people</label>
          <input id="user-search" type="search" placeholder="Name or email" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
      </div>

      {users.error && <ErrorNotice message={users.error} onRetry={users.reload} />}
      {users.loading && !users.data && <Spinner label="Loading people" />}
      {users.data && items.length === 0 && <EmptyState title="No one matches that search" />}

      {items.length > 0 && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Role</th>
                <th>Joined</th>
                <th>
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {items.map((person) => (
                <tr key={person.id}>
                  <td>
                    {person.fullName}
                    <span className="cell-sub">{person.email}</span>
                  </td>
                  <td>
                    <span className={`badge badge--role-${roleLabel(person).toLowerCase()}`}>{roleLabel(person)}</span>
                  </td>
                  <td>{formatDate(person.createdAt)}</td>
                  <td className="actions">
                    <button type="button" className="btn btn--ghost btn--sm" onClick={() => setEditing(person)}>
                      Edit
                    </button>
                    {!person.isAdmin && person.id !== me?.id && (
                      <button type="button" className="btn btn--ghost btn--sm btn--danger-text" onClick={() => setDeleting(person)}>
                        Delete
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editing && (
        <UserForm
          person={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            notify(`Saved ${editing.fullName}.`);
            setEditing(null);
            users.reload();
          }}
        />
      )}

      {deleting && (
        <Modal
          title="Delete this person?"
          onClose={() => setDeleting(null)}
          footer={
            <>
              <button type="button" className="btn btn--ghost" onClick={() => setDeleting(null)} disabled={deleteBusy}>
                Keep account
              </button>
              <button type="button" className="btn btn--danger" onClick={confirmDelete} disabled={deleteBusy}>
                {deleteBusy ? 'Deleting...' : 'Delete account'}
              </button>
            </>
          }
        >
          <p>
            <strong>{deleting.fullName}</strong> ({deleting.email}) will lose access. Accounts with enrollment records, or instructors who still teach a course,
            cannot be deleted.
          </p>
        </Modal>
      )}
    </>
  );
}

function UserForm({ person, onClose, onSaved }: { person: User; onClose: () => void; onSaved: () => void }) {
  const [fullName, setFullName] = useState(person.fullName);
  const [isInstructor, setIsInstructor] = useState(person.isInstructor);
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await usersApi.update(person.id, { fullName, isInstructor });
      onSaved();
    } catch (err) {
      setError(err);
      setBusy(false);
    }
  }

  return (
    <Modal
      title="Edit person"
      onClose={onClose}
      footer={
        <>
          <button type="button" className="btn btn--ghost" onClick={onClose} disabled={busy}>
            Cancel
          </button>
          <button type="submit" form="user-form" className="btn btn--primary" disabled={busy}>
            {busy ? 'Saving...' : 'Save changes'}
          </button>
        </>
      }
    >
      <form id="user-form" onSubmit={handleSubmit} noValidate>
        {error !== null && !fieldError(error, 'fullName') && (
          <div className="notice notice--error" role="alert">
            {errorMessage(error)}
          </div>
        )}
        <Field label="Full name" htmlFor="u-name" error={fieldError(error, 'fullName')}>
          <input id="u-name" value={fullName} onChange={(e) => setFullName(e.target.value)} maxLength={150} />
        </Field>
        <p className="muted">{person.email}</p>
        {!person.isAdmin && (
          <label className="check">
            <input type="checkbox" checked={isInstructor} onChange={(e) => setIsInstructor(e.target.checked)} />
            Can be assigned as a course instructor
          </label>
        )}
      </form>
    </Modal>
  );
}
