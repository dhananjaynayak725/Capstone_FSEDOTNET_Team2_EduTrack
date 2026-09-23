import { useState } from 'react';
import { Link } from 'react-router-dom';
import { enrollmentsApi } from '../api/endpoints';
import type { Enrollment, EnrollmentStatus } from '../api/types';
import { EmptyState, ErrorNotice, PageHeader, ProgressRing, Spinner, StatusBadge } from '../components/Common';
import Modal from '../components/Modal';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { errorMessage, formatDate, formatGrade } from '../utils/format';
import { useLoad } from '../utils/hooks';

type Filter = 'All' | EnrollmentStatus;
const FILTERS: Filter[] = ['All', 'Active', 'Completed', 'Dropped'];

export default function DashboardPage() {
  const { user } = useAuth();
  const { notify } = useToast();
  const enrollments = useLoad(() => enrollmentsApi.mine(), []);

  const [filter, setFilter] = useState<Filter>('All');
  const [dropTarget, setDropTarget] = useState<Enrollment | null>(null);
  const [dropping, setDropping] = useState(false);

  if (enrollments.loading && !enrollments.data) return <Spinner label="Loading your courses" />;
  if (enrollments.error && !enrollments.data) return <ErrorNotice message={enrollments.error} onRetry={enrollments.reload} />;

  const items = enrollments.data ?? [];
  const active = items.filter((item) => item.status === 'Active').length;
  const completed = items.filter((item) => item.status === 'Completed').length;
  const dropped = items.filter((item) => item.status === 'Dropped').length;
  const engaged = active + completed;
  const completion = engaged === 0 ? 0 : Math.round((completed / engaged) * 100);
  const graded = items.filter((item) => item.grade !== null);
  const average = graded.length === 0 ? null : graded.reduce((sum, item) => sum + (item.grade ?? 0), 0) / graded.length;
  const visible = filter === 'All' ? items : items.filter((item) => item.status === filter);

  async function confirmDrop() {
    if (!dropTarget) return;
    setDropping(true);
    try {
      await enrollmentsApi.updateStatus(dropTarget.id, 'Dropped');
      notify(`Dropped ${dropTarget.courseTitle}.`);
      setDropTarget(null);
      enrollments.reload();
    } catch (err) {
      notify(errorMessage(err), 'error');
    } finally {
      setDropping(false);
    }
  }

  return (
    <>
      <PageHeader
        title="My learning"
        intro={`Welcome back, ${user?.fullName.split(' ')[0] ?? 'learner'}.`}
        actions={
          <Link className="btn btn--primary" to="/courses">
            Find a course
          </Link>
        }
      />

      <section className="summary" aria-label="Progress summary">
        <ProgressRing percent={completion} label="completed" />
        <dl className="summary__stats">
          <div>
            <dt>In progress</dt>
            <dd>{active}</dd>
          </div>
          <div>
            <dt>Completed</dt>
            <dd>{completed}</dd>
          </div>
          <div>
            <dt>Dropped</dt>
            <dd>{dropped}</dd>
          </div>
          <div>
            <dt>Average grade</dt>
            <dd>{average === null ? '-' : Number(average.toFixed(1))}</dd>
          </div>
        </dl>
      </section>

      {items.length === 0 ? (
        <EmptyState
          title="You have not enrolled in anything yet"
          action={
            <Link className="btn btn--primary" to="/courses">
              Browse the catalogue
            </Link>
          }
        >
          Pick a course and it will show up here with its status and grade.
        </EmptyState>
      ) : (
        <>
          <div className="tabs" role="tablist" aria-label="Filter by status">
            {FILTERS.map((name) => (
              <button
                key={name}
                type="button"
                role="tab"
                aria-selected={filter === name}
                className={filter === name ? 'tab tab--active' : 'tab'}
                onClick={() => setFilter(name)}
              >
                {name}
              </button>
            ))}
          </div>

          {visible.length === 0 ? (
            <p className="muted">No {filter.toLowerCase()} courses.</p>
          ) : (
            <ul className="rows">
              {visible.map((item) => (
                <li key={item.id} className="row">
                  <div className="row__main">
                    <Link to={`/courses/${item.courseId}`} className="row__title">
                      {item.courseTitle}
                    </Link>
                    <span className="muted">
                      {item.courseCategory}. {item.instructorName}. Enrolled {formatDate(item.enrolledAt)}
                    </span>
                  </div>
                  <div className="row__side">
                    {item.status === 'Completed' && (
                      <span className="row__grade" title="Final grade">
                        {formatGrade(item.grade)}
                      </span>
                    )}
                    <StatusBadge status={item.status} />
                    {item.status === 'Active' && (
                      <button type="button" className="btn btn--ghost btn--sm" onClick={() => setDropTarget(item)}>
                        Drop
                      </button>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </>
      )}

      {dropTarget && (
        <Modal
          title="Drop this course?"
          onClose={() => setDropTarget(null)}
          footer={
            <>
              <button type="button" className="btn btn--ghost" onClick={() => setDropTarget(null)} disabled={dropping}>
                Keep course
              </button>
              <button type="button" className="btn btn--danger" onClick={confirmDrop} disabled={dropping}>
                {dropping ? 'Dropping...' : 'Drop course'}
              </button>
            </>
          }
        >
          <p>
            You will be removed from <strong>{dropTarget.courseTitle}</strong>. You can enroll again later.
          </p>
        </Modal>
      )}
    </>
  );
}
