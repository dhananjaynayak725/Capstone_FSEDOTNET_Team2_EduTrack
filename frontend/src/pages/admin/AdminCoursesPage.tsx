import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { coursesApi, usersApi } from '../../api/endpoints';
import type { Course, CourseInput } from '../../api/types';
import { EmptyState, ErrorNotice, Field, PageHeader, Spinner } from '../../components/Common';
import Modal from '../../components/Modal';
import { useToast } from '../../context/ToastContext';
import { errorMessage, fieldError } from '../../utils/format';
import { useDebounced, useLoad } from '../../utils/hooks';

const EMPTY: CourseInput = { title: '', description: '', category: '', instructorId: 0 };

export default function AdminCoursesPage() {
  const { notify } = useToast();
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounced(search);

  const courses = useLoad(() => coursesApi.list({ search: debouncedSearch }), [debouncedSearch]);
  const instructors = useLoad(() => usersApi.list({ role: 'instructor' }), []);
  const categories = useLoad(() => coursesApi.categories(), []);

  const [editing, setEditing] = useState<Course | 'new' | null>(null);
  const [deleting, setDeleting] = useState<Course | null>(null);
  const [deleteBusy, setDeleteBusy] = useState(false);

  async function confirmDelete() {
    if (!deleting) return;
    setDeleteBusy(true);
    try {
      await coursesApi.remove(deleting.id);
      notify(`Deleted ${deleting.title}.`);
      setDeleting(null);
      courses.reload();
      categories.reload();
    } catch (err) {
      notify(errorMessage(err), 'error');
      setDeleting(null);
    } finally {
      setDeleteBusy(false);
    }
  }

  const items = courses.data ?? [];

  return (
    <>
      <PageHeader
        title="Courses"
        intro="Create, edit and remove courses in the catalogue."
        actions={
          <button type="button" className="btn btn--primary" onClick={() => setEditing('new')}>
            New course
          </button>
        }
      />

      <div className="filters">
        <div className="filters__field filters__field--grow">
          <label htmlFor="course-search">Search courses</label>
          <input id="course-search" type="search" placeholder="Title or description" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
      </div>

      {courses.error && <ErrorNotice message={courses.error} onRetry={courses.reload} />}
      {courses.loading && !courses.data && <Spinner label="Loading courses" />}

      {courses.data && items.length === 0 && (
        <EmptyState title="No courses found">Create the first course to fill the catalogue.</EmptyState>
      )}

      {items.length > 0 && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Course</th>
                <th>Category</th>
                <th>Instructor</th>
                <th className="num">Enrolled</th>
                <th>
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {items.map((course) => (
                <tr key={course.id}>
                  <td>
                    <Link to={`/courses/${course.id}`}>{course.title}</Link>
                  </td>
                  <td>{course.category}</td>
                  <td>{course.instructorName}</td>
                  <td className="num">{course.enrolledCount}</td>
                  <td className="actions">
                    <button type="button" className="btn btn--ghost btn--sm" onClick={() => setEditing(course)}>
                      Edit
                    </button>
                    <button type="button" className="btn btn--ghost btn--sm btn--danger-text" onClick={() => setDeleting(course)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editing && (
        <CourseForm
          course={editing === 'new' ? null : editing}
          instructors={instructors.data ?? []}
          categories={categories.data ?? []}
          onClose={() => setEditing(null)}
          onSaved={(message) => {
            notify(message);
            setEditing(null);
            courses.reload();
            categories.reload();
          }}
        />
      )}

      {deleting && (
        <Modal
          title="Delete this course?"
          onClose={() => setDeleting(null)}
          footer={
            <>
              <button type="button" className="btn btn--ghost" onClick={() => setDeleting(null)} disabled={deleteBusy}>
                Keep course
              </button>
              <button type="button" className="btn btn--danger" onClick={confirmDelete} disabled={deleteBusy}>
                {deleteBusy ? 'Deleting...' : 'Delete course'}
              </button>
            </>
          }
        >
          <p>
            <strong>{deleting.title}</strong> will be removed from the catalogue. Courses that already have enrollment records cannot be deleted.
          </p>
        </Modal>
      )}
    </>
  );
}

interface CourseFormProps {
  course: Course | null;
  instructors: { id: number; fullName: string }[];
  categories: string[];
  onClose: () => void;
  onSaved: (message: string) => void;
}

function CourseForm({ course, instructors, categories, onClose, onSaved }: CourseFormProps) {
  const [form, setForm] = useState<CourseInput>(
    course
      ? { title: course.title, description: course.description, category: course.category, instructorId: course.instructorId }
      : EMPTY,
  );
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);

  const set = <K extends keyof CourseInput>(key: K, value: CourseInput[K]) => setForm((current) => ({ ...current, [key]: value }));
  const hasFieldErrors = ['title', 'category', 'instructorId', 'description'].some((key) => fieldError(error, key));

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      if (course) {
        await coursesApi.update(course.id, form);
        onSaved(`Saved ${form.title.trim()}.`);
      } else {
        await coursesApi.create(form);
        onSaved(`Created ${form.title.trim()}.`);
      }
    } catch (err) {
      setError(err);
      setBusy(false);
    }
  }

  return (
    <Modal
      title={course ? 'Edit course' : 'New course'}
      onClose={onClose}
      footer={
        <>
          <button type="button" className="btn btn--ghost" onClick={onClose} disabled={busy}>
            Cancel
          </button>
          <button type="submit" form="course-form" className="btn btn--primary" disabled={busy}>
            {busy ? 'Saving...' : course ? 'Save changes' : 'Create course'}
          </button>
        </>
      }
    >
      <form id="course-form" onSubmit={handleSubmit} noValidate>
        {error !== null && !hasFieldErrors && (
          <div className="notice notice--error" role="alert">
            {errorMessage(error)}
          </div>
        )}
        <Field label="Title" htmlFor="c-title" error={fieldError(error, 'title')}>
          <input id="c-title" value={form.title} onChange={(e) => set('title', e.target.value)} maxLength={200} />
        </Field>
        <Field label="Category" htmlFor="c-category" error={fieldError(error, 'category')} hint="Pick an existing category or type a new one.">
          <input id="c-category" list="category-options" value={form.category} onChange={(e) => set('category', e.target.value)} maxLength={100} />
          <datalist id="category-options">
            {categories.map((name) => (
              <option key={name} value={name} />
            ))}
          </datalist>
        </Field>
        <Field label="Instructor" htmlFor="c-instructor" error={fieldError(error, 'instructorId')}>
          <select id="c-instructor" value={form.instructorId} onChange={(e) => set('instructorId', Number(e.target.value))}>
            <option value={0}>Choose an instructor</option>
            {instructors.map((person) => (
              <option key={person.id} value={person.id}>
                {person.fullName}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Description" htmlFor="c-desc" error={fieldError(error, 'description')}>
          <textarea id="c-desc" rows={4} value={form.description} onChange={(e) => set('description', e.target.value)} maxLength={2000} />
        </Field>
      </form>
    </Modal>
  );
}
