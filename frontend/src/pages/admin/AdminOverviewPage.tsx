import { useState } from 'react';
import { adminApi } from '../../api/endpoints';
import { ErrorNotice, PageHeader, Spinner } from '../../components/Common';
import { useToast } from '../../context/ToastContext';
import { errorMessage, formatMonth } from '../../utils/format';
import { useLoad } from '../../utils/hooks';

export default function AdminOverviewPage() {
  const dashboard = useLoad(() => adminApi.dashboard(), []);
  const { notify } = useToast();
  const [exporting, setExporting] = useState(false);

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

  if (dashboard.loading && !dashboard.data) return <Spinner label="Loading dashboard" />;
  if (dashboard.error || !dashboard.data) {
    return <ErrorNotice message={dashboard.error ?? 'Could not load the dashboard.'} onRetry={dashboard.reload} />;
  }

  const d = dashboard.data;
  const statusTotal = Math.max(d.totalEnrollments, 1);
  const topMax = Math.max(...d.topCourses.map((course) => course.count), 1);
  const trendMax = Math.max(...d.enrollmentTrend.map((point) => point.count), 1);

  const segments = [
    { key: 'active', label: 'Active', value: d.activeEnrollments },
    { key: 'completed', label: 'Completed', value: d.completedEnrollments },
    { key: 'dropped', label: 'Dropped', value: d.droppedEnrollments },
  ];

  return (
    <>
      <PageHeader
        title="Overview"
        intro="Enrollment activity across all courses."
        actions={
          <button type="button" className="btn btn--ghost" onClick={exportCsv} disabled={exporting}>
            {exporting ? 'Preparing file...' : 'Export enrollments (CSV)'}
          </button>
        }
      />

      <dl className="stats">
        <div>
          <dt>Students</dt>
          <dd>{d.totalStudents}</dd>
        </div>
        <div>
          <dt>Instructors</dt>
          <dd>{d.totalInstructors}</dd>
        </div>
        <div>
          <dt>Courses</dt>
          <dd>{d.totalCourses}</dd>
        </div>
        <div>
          <dt>Enrollments</dt>
          <dd>{d.totalEnrollments}</dd>
        </div>
        <div>
          <dt>Average grade</dt>
          <dd>{d.averageGrade === null ? '-' : d.averageGrade}</dd>
        </div>
      </dl>

      <div className="grid-2">
        <section className="panel">
          <h2>Enrollment status</h2>
          <div className="split" role="img" aria-label={segments.map((s) => `${s.label} ${s.value}`).join(', ')}>
            {segments.map((segment) =>
              segment.value > 0 ? (
                <span
                  key={segment.key}
                  className={`split__part split__part--${segment.key}`}
                  style={{ width: `${(segment.value / statusTotal) * 100}%` }}
                />
              ) : null,
            )}
          </div>
          <ul className="legend">
            {segments.map((segment) => (
              <li key={segment.key}>
                <span className={`legend__dot legend__dot--${segment.key}`} aria-hidden="true" />
                {segment.label}
                <strong>{segment.value}</strong>
              </li>
            ))}
          </ul>
        </section>

        <section className="panel">
          <h2>Most popular courses</h2>
          {d.topCourses.length === 0 ? (
            <p className="muted">No enrollments yet.</p>
          ) : (
            <ul className="bars">
              {d.topCourses.map((course) => (
                <li key={course.courseId}>
                  <span className="bars__label" title={course.title}>
                    {course.title}
                  </span>
                  <span className="bars__track">
                    <span className="bars__fill" style={{ width: `${(course.count / topMax) * 100}%` }} />
                  </span>
                  <strong>{course.count}</strong>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>

      <section className="panel">
        <h2>New enrollments, last 6 months</h2>
        <div className="trend">
          {d.enrollmentTrend.map((point) => (
            <div key={point.month} className="trend__col">
              <span className="trend__value">{point.count}</span>
              <span className="trend__bar" style={{ height: `${Math.max((point.count / trendMax) * 100, 3)}%` }} />
              <span className="trend__label">{formatMonth(point.month)}</span>
            </div>
          ))}
        </div>
      </section>
    </>
  );
}
