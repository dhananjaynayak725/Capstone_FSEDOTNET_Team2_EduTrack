import { Link } from 'react-router-dom';
import type { Course } from '../api/types';
import { categoryColor } from '../utils/format';

export default function CourseTile({ course }: { course: Course }) {
  const color = categoryColor(course.category);
  return (
    <Link to={`/courses/${course.id}`} className="tile" style={{ ['--spine' as string]: color }}>
      <span className="tile__category">{course.category}</span>
      <h3 className="tile__title">{course.title}</h3>
      <p className="tile__desc">{course.description || 'No description yet.'}</p>
      <div className="tile__foot">
        <span>{course.instructorName}</span>
        <span>{course.enrolledCount} enrolled</span>
      </div>
    </Link>
  );
}
