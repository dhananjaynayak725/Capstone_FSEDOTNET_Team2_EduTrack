import { useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { coursesApi, enrollmentsApi } from '../api/endpoints';
import { ErrorNotice, Spinner, StatusBadge } from '../components/Common';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { categoryColor, errorMessage, formatDate, formatGrade } from '../utils/format';
import { useLoad } from '../utils/hooks';

type Step = 'idle' | 'confirm' | 'done';

export default function CourseDetailPage() {
  const { id } = useParams();
  const courseId = Number(id);
  const { user } = useAuth();
  const location = useLocation();
  const { notify } = useToast();

  const course = useLoad(() => coursesApi.get(courseId), [courseId]);
  const mine = useLoad(async () => (user && !user.isAdmin ? enrollmentsApi.mine() : []), [user?.id]);

  const [step, setStep] = useState<Step>('idle');
  const [busy, setBusy] = useState(false);
  const [enrollError, setEnrollError] = useState<string | null>(null);

  if (Number.isNaN(courseId)) return <ErrorNotice message="That course link is not valid." />;
  if (course.loading && !course.data) return <Spinner label="Loading course" />;
  if (course.error || !course.data) {
    return <ErrorNotice message={course.error ?? 'Course not found.'} onRetry={course.reload} />;
  }

  const item = course.data;
  const enrollment = (mine.data ?? []).find((entry) => entry.courseId === item.id);

  async function confirmEnrollment() {
    setBusy(true);
    setEnrollError(null);
    try {
      await enrollmentsApi.enroll(item.id);
      setStep('done');
      notify(`Enrolled in ${item.title}.`);
      mine.reload();
      course.reload();
    } catch (err) {
      setEnrollError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  function renderEnrollment() {
    if (!user) {
      return (
        <>
          <h2>Enroll</h2>
          <p className="muted">Sign in or create an account to enroll in this course.</p>
          <Link className="btn btn--primary btn--block" to="/login" state={{ from: location.pathname }}>
            Log in to enroll
          </Link>
          <Link className="btn btn--ghost btn--block" to="/register">
            Create an account
          </Link>
        </>
      );
    }

    if (user.isAdmin) {
      return (
        <>
          <h2>Admin view</h2>
          <p className="muted">Admins manage courses and enrollments instead of enrolling.</p>
          <Link className="btn btn--primary btn--block" to="/admin/courses">
            Manage courses
          </Link>
        </>
      );
    }

    if (step === 'done') {
      return (
        <>
          <h2>You are enrolled</h2>
          <p>
            <strong>{item.title}</strong> is now on your dashboard.
          </p>
          <Link className="btn btn--primary btn--block" to="/dashboard">
            Go to my learning
          </Link>
          <Link className="btn btn--ghost btn--block" to="/courses">
            Browse more courses
          </Link>
        </>
      );
    }

    if (mine.loading && !mine.data) return <Spinner label="Checking your enrollment" />;

    if (enrollment && enrollment.status !== 'Dropped') {
      return (
        <>
          <h2>Your enrollment</h2>
          <p className="enroll__status">
            <StatusBadge status={enrollment.status} />
            <span className="muted">since {formatDate(enrollment.enrolledAt)}</span>
          </p>
          {enrollment.status === 'Completed' && (
            <p>
              Final grade: <strong>{formatGrade(enrollment.grade)}</strong>
            </p>
          )}
          <Link className="btn btn--primary btn--block" to="/dashboard">
            Go to my learning
          </Link>
        </>
      );
    }

    if (step === 'confirm') {
      return (
        <>
          <h2>Confirm enrollment</h2>
          <p>
            Enroll in <strong>{item.title}</strong> with {item.instructorName}? You can drop it later from your dashboard.
          </p>
          {enrollError && (
            <div className="notice notice--error" role="alert">
              {enrollError}
            </div>
          )}
          <button type="button" className="btn btn--primary btn--block" onClick={confirmEnrollment} disabled={busy}>
            {busy ? 'Enrolling...' : 'Confirm enrollment'}
          </button>
          <button type="button" className="btn btn--ghost btn--block" onClick={() => setStep('idle')} disabled={busy}>
            Cancel
          </button>
        </>
      );
    }

    return (
      <>
        <h2>{enrollment ? 'Enroll again' : 'Enroll'}</h2>
        <p className="muted">
          {enrollment
            ? 'You dropped this course earlier. Enrolling again restarts it from today.'
            : 'Join the learners already taking this course.'}
        </p>
        <button type="button" className="btn btn--primary btn--block" onClick={() => setStep('confirm')}>
          {enrollment ? 'Re-enroll' : 'Enroll in this course'}
        </button>
      </>
    );
  }

  return (
    <div className="detail">
      <article className="detail__main" style={{ ['--spine' as string]: categoryColor(item.category) }}>
        <Link to="/courses" className="backlink">
          All courses
        </Link>
        <span className="tile__category">{item.category}</span>
        <h1>{item.title}</h1>
        <p className="detail__meta">
          Taught by <strong>{item.instructorName}</strong>. {item.enrolledCount} {item.enrolledCount === 1 ? 'learner' : 'learners'} enrolled. Added{' '}
          {formatDate(item.createdAt)}.
        </p>
        <p className="detail__desc">{item.description || 'The instructor has not added a description yet.'}</p>
      </article>

      <aside className="panel detail__enroll">{renderEnrollment()}</aside>
    </div>
  );
}
