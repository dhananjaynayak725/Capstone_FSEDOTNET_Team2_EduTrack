import { Link } from 'react-router-dom';
import { EmptyState } from '../components/Common';

export default function NotFoundPage() {
  return (
    <EmptyState
      title="We could not find that page"
      action={
        <Link className="btn btn--primary" to="/courses">
          Back to courses
        </Link>
      }
    >
      The link may be old, or the page may have moved.
    </EmptyState>
  );
}
