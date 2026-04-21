import { Link, NavLink } from 'react-router-dom'

function NavItem({ to, children }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        `rounded-full px-3 py-1 text-sm transition ${
          isActive
            ? 'bg-white/80 text-ink-900 shadow-sm ring-1 ring-black/5'
            : 'text-ink-700 hover:bg-white/60 hover:text-ink-900'
        }`
      }
    >
      {children}
    </NavLink>
  )
}

export default function Layout({ children }) {
  return (
    <div className="min-h-full">
      <header className="sticky top-0 z-10 border-b border-black/5 bg-white/70 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
          <Link to="/orders" className="group inline-flex items-baseline gap-2">
            <span className="text-lg font-semibold tracking-tight text-ink-900">
              Order Management
            </span>
            <span className="rounded-full bg-teal-50 px-2 py-0.5 text-xs font-medium text-teal-700 ring-1 ring-teal-200">
              MVP
            </span>
          </Link>

          <nav className="flex items-center gap-2 rounded-full bg-white/60 p-1 ring-1 ring-black/5">
            <NavItem to="/orders">Orders</NavItem>
            <NavItem to="/orders/new">New</NavItem>
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-8">{children}</main>

      <footer className="mx-auto max-w-6xl px-4 pb-10 pt-6 text-xs text-ink-700">
        <div className="flex flex-wrap items-center justify-between gap-2 border-t border-black/5 pt-4">
          <span>Local dev UI for technical test.</span>
          <span className="font-mono text-[11px] opacity-70">
            API: {import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'}
          </span>
        </div>
      </footer>
    </div>
  )
}

