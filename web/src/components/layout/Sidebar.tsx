import type { LucideIcon } from 'lucide-react'
import {
  ChevronsLeft,
  ChevronsRight,
  Compass,
  Flame,
  Library,
  Monitor,
  Moon,
  House,
  Settings,
  ShieldCheck,
  Smartphone,
  Sun,
  X,
} from 'lucide-react'
import { NavLink, useLocation } from 'react-router-dom'
import { useTheme } from '../../context/ThemeContext'
import { groupCalculators } from '../../config/calculators'

interface SidebarProps {
  isOpen: boolean
  onClose: () => void
  isCollapsed: boolean
  onToggleCollapse: () => void
}

const PRIMARY_LINKS: { to: string; label: string; icon: LucideIcon }[] = [
  { to: '/', label: 'Home', icon: House },
  { to: '/quiz', label: 'Find Your Path', icon: Compass },
  { to: '/books', label: 'Recommended Books', icon: Library },
  { to: '/apps', label: 'Recommended Apps', icon: Smartphone },
]

const CALCULATOR_GROUPS = groupCalculators()

const THEMES: { value: 'light' | 'dark' | 'system'; label: string; icon: LucideIcon }[] = [
  { value: 'light', label: 'Light', icon: Sun },
  { value: 'dark', label: 'Dark', icon: Moon },
  { value: 'system', label: 'System', icon: Monitor },
]

function navClass(isActive: boolean, isCollapsed: boolean) {
  return [
    'flex items-center gap-3 rounded-control px-3 py-2 text-sm font-medium',
    'transition-colors motion-reduce:transition-none',
    'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface-raised',
    isCollapsed ? 'justify-center' : '',
    isActive
      ? 'bg-accent-subtle text-accent'
      : 'text-content-muted hover:bg-surface-sunken hover:text-content',
  ].join(' ')
}

export default function Sidebar({ isOpen, onClose, isCollapsed, onToggleCollapse }: SidebarProps) {
  const location = useLocation()
  const { theme, setTheme } = useTheme()

  // Preserve query parameters when navigating between calculators
  const currentSearch = location.search
  const CollapseIcon = isCollapsed ? ChevronsRight : ChevronsLeft

  return (
    <aside
      id="sidebar-navigation"
      className={`
        fixed lg:sticky top-0 left-0 z-50 h-screen
        bg-surface-raised border-r border-border-subtle
        flex flex-col
        transform transition-all duration-200 ease-in-out
        motion-reduce:transition-none
        ${isOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
        ${isCollapsed ? 'lg:w-20' : 'w-72'}
      `}
    >
      <div className="flex h-16 items-center justify-between gap-2 border-b border-border-subtle px-4">
        <NavLink
          to="/"
          onClick={onClose}
          className={`flex min-w-0 items-center gap-2.5 rounded-control focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
            isCollapsed ? 'w-full justify-center' : ''
          }`}
        >
          <Flame className="h-6 w-6 shrink-0 text-accent" aria-hidden="true" strokeWidth={1.5} />
          {!isCollapsed && (
            <span className="min-w-0">
              <span className="block truncate text-sm font-semibold text-content">FIRE Calculators</span>
              <span className="block truncate text-xs text-content-subtle">Financial Independence</span>
            </span>
          )}
        </NavLink>

        <button
          onClick={onClose}
          className="lg:hidden rounded-control p-2 text-content-muted transition-colors hover:bg-surface-sunken hover:text-content motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label="Close menu"
        >
          <X className="h-5 w-5" aria-hidden="true" strokeWidth={1.5} />
        </button>
        <button
          onClick={onToggleCollapse}
          className="hidden lg:block rounded-control p-2 text-content-muted transition-colors hover:bg-surface-sunken hover:text-content motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          aria-expanded={!isCollapsed}
        >
          <CollapseIcon className="h-5 w-5" aria-hidden="true" strokeWidth={1.5} />
        </button>
      </div>

      <nav className="flex-1 space-y-1 overflow-y-auto p-3">
        {PRIMARY_LINKS.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === '/'}
            onClick={onClose}
            className={({ isActive }) => navClass(isActive, isCollapsed)}
            title={isCollapsed ? label : undefined}
            aria-label={isCollapsed ? label : undefined}
          >
            <Icon className="h-5 w-5 shrink-0" aria-hidden="true" strokeWidth={1.5} />
            {!isCollapsed && <span className="truncate">{label}</span>}
          </NavLink>
        ))}

        {CALCULATOR_GROUPS.map(({ category, items }) => (
          <div key={category.id} role="group" aria-labelledby={`sidebar-group-${category.id}`}>
            {!isCollapsed ? (
              <h2
                id={`sidebar-group-${category.id}`}
                className="px-3 pt-5 pb-1 text-[0.6875rem] font-semibold uppercase tracking-[0.08em] text-content-subtle"
              >
                {category.label}
              </h2>
            ) : (
              <>
                <span id={`sidebar-group-${category.id}`} className="sr-only">{category.label}</span>
                <div className="mx-2 my-3 border-t border-border-subtle" />
              </>
            )}

            {items.map(calc => {
              const Icon = calc.icon
              return (
                <NavLink
                  key={calc.path}
                  to={`${calc.path}${currentSearch}`}
                  onClick={onClose}
                  className={({ isActive }) => navClass(isActive, isCollapsed)}
                  title={isCollapsed ? calc.label : undefined}
                  aria-label={isCollapsed ? calc.label : undefined}
                >
                  <Icon className={`h-5 w-5 shrink-0 ${calc.accent}`} aria-hidden="true" strokeWidth={1.5} />
                  {!isCollapsed && <span className="truncate">{calc.label}</span>}
                </NavLink>
              )
            })}
          </div>
        ))}
      </nav>

      <div className="space-y-2 border-t border-border-subtle p-3">
        <div
          role="radiogroup"
          aria-label="Colour theme"
          className={`flex gap-1 rounded-control bg-surface-sunken p-1 ${
            isCollapsed ? 'flex-col items-center' : ''
          }`}
        >
          {THEMES.map(({ value, label, icon: Icon }) => {
            const selected = theme === value
            return (
              <button
                key={value}
                type="button"
                role="radio"
                aria-checked={selected}
                onClick={() => setTheme(value)}
                title={label}
                className={`flex flex-1 items-center justify-center gap-1.5 rounded-[calc(var(--radius-control)-0.25rem)] px-2 py-1.5 text-xs font-medium transition-colors motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring ${
                  selected
                    ? 'bg-surface-raised text-content shadow-[0_1px_2px_rgb(0_0_0/0.06)]'
                    : 'text-content-subtle hover:text-content'
                }`}
              >
                <Icon className="h-4 w-4 shrink-0" aria-hidden="true" strokeWidth={1.5} />
                {!isCollapsed && <span>{label}</span>}
                {isCollapsed && <span className="sr-only">{label}</span>}
              </button>
            )
          })}
        </div>

        <NavLink
          to="/settings"
          onClick={onClose}
          className={({ isActive }) => navClass(isActive, isCollapsed)}
          title={isCollapsed ? 'Settings' : undefined}
          aria-label={isCollapsed ? 'Settings' : undefined}
        >
          <Settings className="h-5 w-5 shrink-0" aria-hidden="true" strokeWidth={1.5} />
          {!isCollapsed && <span className="truncate">Settings</span>}
        </NavLink>

        <p
          className={`flex items-center gap-2 rounded-control px-2.5 py-1.5 text-xs font-medium text-content-muted ${
            isCollapsed ? 'justify-center' : ''
          }`}
        >
          <ShieldCheck className="h-4 w-4 shrink-0 text-success" aria-hidden="true" strokeWidth={1.5} />
          {isCollapsed ? (
            <span className="sr-only">100% private and offline</span>
          ) : (
            <span>100% private and offline</span>
          )}
        </p>
      </div>
    </aside>
  )
}
