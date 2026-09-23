import { NavLink, Outlet } from 'react-router-dom';

const LINKS = [
  { to: '/admin', label: 'Overview', end: true },
  { to: '/admin/courses', label: 'Courses', end: false },
  { to: '/admin/enrollments', label: 'Enrollments', end: false },
  { to: '/admin/users', label: 'People', end: false },
];

export default function AdminLayout() {
  return (
    <>
      <nav className="subnav" aria-label="Admin sections">
        {LINKS.map((link) => (
          <NavLink key={link.to} to={link.to} end={link.end}>
            {link.label}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </>
  );
}
