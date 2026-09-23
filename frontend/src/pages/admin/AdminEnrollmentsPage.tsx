import { useEffect, useState } from 'react';
import { adminApi, coursesApi, enrollmentsApi } from '../../api/endpoints';
import type { Enrollment, EnrollmentStatus } from '../../api/types';
import { EmptyState, ErrorNotice, PageHeader, Spinner } from '../../components/Common';
import { useToast } from '../../context/ToastContext';
import { errorMessage, formatDate } from '../../utils/format';
import { useLoad } from '../../utils/hooks';

const STATUSES: EnrollmentStatus[] = ['Active', 'Completed', 'Dropped'];

export default function AdminEnrollmentsPage() {
  const { notify } = useToast();
  const [courseId, setCourseId] = useState('');
  const [status, setStatus] = useState<'' | EnrollmentStatus>('');
  const [exporting, setExporting] = useState(false);

  const courses = useLoad(() => coursesApi.list(), []);
  const enrollments = useLoad(
    () => enrollmentsApi.all({ courseId: courseId ? Number(courseId) : undefined, status: status || undefined }),
    [courseId, status],
  );

  function replace(updated: Enrollment) {
    enrollments.setData((current) => (current ? current.map((item) => (item.id === updated.id ? updated : item)) : current));
  }

  async function exportCsv() {
    setExporting(true);
    try {
      await adminApi.exportEnrollments();
    } catch (err) {
      notify(errorMessage(err), 'error');
    } finally {
      setExporting(false);
    }
  }

  const items = enrollments.data ?? [];

  return (
    <>
      <PageHeader
        title="Enrollments"
        intro="Change a status or record a final grade. Grades can only be set on completed enrollments."
        actions={
          <button type="button" className="btn btn--ghost" onClick={exportCsv} disabled={exporting}>
            {exporting ? 'Preparing file...' : 'Export CSV'}
          </button>
        }
      />

      <div className="filters">
        <div className="filters__field filters__field--grow">
          <label htmlFor="f-course">Course</label>
          <select id="f-course" value={courseId} onChange={(e) => setCourseId(e.target.value)}>
            <option value="">All courses</option>
            {(courses.data ?? []).map((course) => (
              <option key={course.id} value={course.id}>
                {course.title}
              </option>
            ))}
          </select>
        </div>
        <div className="filters__field">
          <label htmlFor="f-status">Status</label>
          <select id="f-status" value={status} onChange={(e) => setStatus(e.target.value as '' | EnrollmentStatus)}>
            <option value="">Any status</option>
            {STATUSES.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {enrollments.error && <ErrorNotice message={enrollments.error} onRetry={enrollments.reload} />}
      {enrollments.loading && !enrollments.data && <Spinner label="Loading enrollments" />}
      {enrollments.data && items.length === 0 && <EmptyState title="No enrollments match these filters" />}

      {items.length > 0 && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Student</th>
                <th>Course</th>
                <th>Enrolled</th>
                <th>Status</th>
                <th>Grade</th>
                <th>
                  <span className="sr-only">Save</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <EnrollmentRow key={item.id} item={item} onSaved={replace} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}

function EnrollmentRow({ item, onSaved }: { item: Enrollment; onSaved: (updated: Enrollment) => void }) {
  const { notify } = useToast();
  const savedGrade = item.grade === null ? '' : String(item.grade);

  const [status, setStatus] = useState<EnrollmentStatus>(item.status);
  const [grade, setGrade] = useState(savedGrade);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setStatus(item.status);
    setGrade(item.grade === null ? '' : String(item.grade));
  }, [item.status, item.grade]);

  const dirty = status !== item.status || (status === 'Completed' && grade.trim() !== savedGrade);

  async function save() {
    let value: number | null = null;
    if (status === 'Completed' && grade.trim() !== '') {
      value = Number(grade);
      if (Number.isNaN(value) || value < 0 || value > 100) {
        notify('Enter a grade between 0 and 100.', 'error');
        return;
      }
    }

    setBusy(true);
    try {
      const updated = await enrollmentsApi.updateStatus(item.id, status, value);
      onSaved(updated);
      notify(`Updated ${item.studentName}'s enrollment.`);
    } catch (err) {
      notify(errorMessage(err), 'error');
    } finally {
      setBusy(false);
    }
  }

  return (
    <tr>
      <td>
        {item.studentName}
        <span className="cell-sub">{item.studentEmail}</span>
      </td>
      <td>{item.courseTitle}</td>
      <td>{formatDate(item.enrolledAt)}</td>
      <td>
        <select aria-label={`Status for ${item.studentName} in ${item.courseTitle}`} value={status} onChange={(e) => setStatus(e.target.value as EnrollmentStatus)}>
          {STATUSES.map((name) => (
            <option key={name} value={name}>
              {name}
            </option>
          ))}
        </select>
      </td>
      <td>
        <input
          className="grade-input"
          type="number"
          min={0}
          max={100}
          step="0.5"
          inputMode="decimal"
          aria-label={`Grade for ${item.studentName} in ${item.courseTitle}`}
          value={status === 'Completed' ? grade : ''}
          placeholder={status === 'Completed' ? 'Grade' : '-'}
          disabled={status !== 'Completed'}
          onChange={(e) => setGrade(e.target.value)}
        />
      </td>
      <td className="actions">
        <button type="button" className="btn btn--primary btn--sm" onClick={save} disabled={!dirty || busy}>
          {busy ? 'Saving...' : 'Save'}
        </button>
      </td>
    </tr>
  );
}
