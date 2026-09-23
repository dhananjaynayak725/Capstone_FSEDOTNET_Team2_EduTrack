import { useState } from 'react';
import { coursesApi } from '../api/endpoints';
import { EmptyState, ErrorNotice, PageHeader, Spinner } from '../components/Common';
import CourseTile from '../components/CourseTile';
import { useDebounced, useLoad } from '../utils/hooks';

export default function CoursesPage() {
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [instructor, setInstructor] = useState('');

  const debouncedSearch = useDebounced(search);
  const debouncedInstructor = useDebounced(instructor);

  const categories = useLoad(() => coursesApi.categories(), []);
  const courses = useLoad(
    () => coursesApi.list({ search: debouncedSearch, category, instructor: debouncedInstructor }),
    [debouncedSearch, category, debouncedInstructor],
  );

  const filtersActive = search !== '' || category !== '' || instructor !== '';
  const items = courses.data ?? [];

  function clearFilters() {
    setSearch('');
    setCategory('');
    setInstructor('');
  }

  return (
    <>
      <PageHeader
        title="Find your next course"
        intro="Browse the catalogue, then enroll in a click. Sign in to track progress and grades."
      />

      <form className="filters" role="search" onSubmit={(event) => event.preventDefault()}>
        <div className="filters__field filters__field--grow">
          <label htmlFor="search">Course name</label>
          <input id="search" type="search" placeholder="Try SQL, React or Azure" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <div className="filters__field">
          <label htmlFor="category">Category</label>
          <select id="category" value={category} onChange={(e) => setCategory(e.target.value)}>
            <option value="">All categories</option>
            {(categories.data ?? []).map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
        </div>
        <div className="filters__field">
          <label htmlFor="instructor">Instructor</label>
          <input id="instructor" type="search" placeholder="Any instructor" value={instructor} onChange={(e) => setInstructor(e.target.value)} />
        </div>
        {filtersActive && (
          <button type="button" className="btn btn--ghost" onClick={clearFilters}>
            Clear filters
          </button>
        )}
      </form>

      {courses.error && <ErrorNotice message={courses.error} onRetry={courses.reload} />}

      {courses.loading && courses.data === null && <Spinner label="Loading courses" />}

      {courses.data !== null && (
        <>
          <p className="result-count" aria-live="polite">
            {items.length} {items.length === 1 ? 'course' : 'courses'}
            {filtersActive ? ' match your filters' : ' available'}
          </p>

          {items.length === 0 ? (
            <EmptyState
              title="No courses match these filters"
              action={
                <button type="button" className="btn btn--primary" onClick={clearFilters}>
                  Clear filters
                </button>
              }
            >
              Try a shorter search, or pick a different category.
            </EmptyState>
          ) : (
            <div className="tiles">
              {items.map((course) => (
                <CourseTile key={course.id} course={course} />
              ))}
            </div>
          )}
        </>
      )}
    </>
  );
}
