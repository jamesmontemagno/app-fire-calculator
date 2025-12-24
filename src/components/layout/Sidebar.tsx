import { NavLink, useLocation } from 'react-router-dom'
import { useTheme } from '../../context/ThemeContext'

interface SidebarProps {
  isOpen: boolean
  onClose: () => void
  isCollapsed: boolean
  onToggleCollapse: () => void
}

const calculators = [
  { path: '/standard', icon: '🎯', label: 'Standard FIRE', color: 'text-orange-500' },
  { path: '/coast', icon: '⛵', label: 'Coast FIRE', color: 'text-blue-500' },
  { path: '/lean', icon: '🌿', label: 'Lean FIRE', color: 'text-green-500' },
  { path: '/fat', icon: '💎', label: 'Fat FIRE', color: 'text-purple-500' },
  { path: '/barista', icon: '☕', label: 'Barista FIRE', color: 'text-amber-600' },
  { path: '/reverse', icon: '🔄', label: 'Reverse FIRE', color: 'text-teal-500' },
  { path: '/withdrawal', icon: '📊', label: 'Withdrawal Rate', color: 'text-sky-500' },
  { path: '/savings-rate', icon: '🧮', label: 'Savings & Investment Rate', color: 'text-indigo-500' },
  { path: '/debt-payoff', icon: '💳', label: 'Debt Payoff', color: 'text-red-500' },
  { path: '/healthcare', icon: '🏥', label: 'Healthcare Gap', color: 'text-rose-500' },
]

export default function Sidebar({ isOpen, onClose, isCollapsed, onToggleCollapse }: SidebarProps) {
  const location = useLocation()
  const { theme, setTheme } = useTheme()

  const isHome = location.pathname === '/'
  
  // Preserve query parameters when navigating between calculators
  const currentSearch = location.search

  return (
    <aside 
      className={`
        fixed lg:sticky top-0 left-0 z-50 h-screen
        bg-white dark:bg-gray-900 
        border-r border-gray-200 dark:border-gray-800
        flex flex-col
        transform transition-all duration-200 ease-in-out
        ${isOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
        ${isCollapsed ? 'lg:w-20' : 'w-72'}
      `}
    >
      {/* Logo */}
      <div className="flex items-center justify-between h-16 px-4 border-b border-gray-200 dark:border-gray-800">
        {!isCollapsed ? (
          <NavLink 
            to="/" 
            className="flex items-center gap-3 hover:opacity-80 transition-opacity"
            onClick={onClose}
          >
            <span className="text-3xl">🔥</span>
            <div>
              <div className="font-bold text-lg text-gray-900 dark:text-gray-100">FIRE Calculators</div>
              <div className="text-xs text-gray-500 dark:text-gray-400">Financial Independence</div>
            </div>
          </NavLink>
        ) : (
          <NavLink 
            to="/" 
            className="flex items-center justify-center w-full hover:opacity-80 transition-opacity"
            onClick={onClose}
          >
            <span className="text-3xl">🔥</span>
          </NavLink>
        )}
        <button
          onClick={onClose}
          className="lg:hidden p-2 text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg transition-colors"
          aria-label="Close menu"
        >
          <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
        <button
          onClick={onToggleCollapse}
          className="hidden lg:block p-2 text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg transition-colors"
          aria-label={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
          title={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
        >
          <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            {isCollapsed ? (
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 5l7 7-7 7M5 5l7 7-7 7" />
            ) : (
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 19l-7-7 7-7m8 14l-7-7 7-7" />
            )}
          </svg>
        </button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto p-4">
        {/* Home link */}
        <NavLink
          to="/"
          onClick={onClose}
          className={`
            flex items-center gap-3 px-3 py-2.5 rounded-lg mb-1
            font-medium transition-colors
            ${isCollapsed ? 'justify-center' : ''}
            ${isHome 
              ? 'bg-fire-100 dark:bg-fire-900/30 text-fire-700 dark:text-fire-400' 
              : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
            }
          `}
          title={isCollapsed ? 'Home' : ''}
        >
          <span className="text-xl">🏠</span>
          {!isCollapsed && <span>Home</span>}
        </NavLink>

        {/* Recommended Books link */}
        <NavLink
          to="/books"
          onClick={onClose}
          className={({ isActive }) => `
            flex items-center gap-3 px-3 py-2.5 rounded-lg mb-1
            font-medium transition-colors
            ${isCollapsed ? 'justify-center' : ''}
            ${isActive 
              ? 'bg-fire-100 dark:bg-fire-900/30 text-fire-700 dark:text-fire-400' 
              : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
            }
          `}
          title={isCollapsed ? 'Recommended Books' : ''}
        >
          <span className="text-xl">📚</span>
          {!isCollapsed && <span>Recommended Books</span>}
        </NavLink>

        {/* Quiz link */}
        <NavLink
          to="/quiz"
          onClick={onClose}
          className={({ isActive }) => `
            flex items-center gap-3 px-3 py-2.5 rounded-lg mb-2
            font-medium transition-colors
            ${isCollapsed ? 'justify-center' : ''}
            ${isActive 
              ? 'bg-fire-100 dark:bg-fire-900/30 text-fire-700 dark:text-fire-400' 
              : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
            }
          `}
          title={isCollapsed ? 'Find Your Path' : ''}
        >
          <span className="text-xl">🧭</span>
          {!isCollapsed && <span>Find Your Path</span>}
        </NavLink>

        {!isCollapsed && (
          <div className="mt-4 mb-3 px-3">
            <h3 className="text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
              Calculators
            </h3>
          </div>
        )}
        {isCollapsed && <div className="border-t border-gray-200 dark:border-gray-700 my-2"></div>}

        <div className="space-y-1">
          {calculators.map(calc => (
            <NavLink
              key={calc.path}
              to={`${calc.path}${currentSearch}`}
              onClick={onClose}
              className={({ isActive }) => `
                flex items-center gap-3 px-3 py-2.5 rounded-lg
                font-medium transition-colors
                ${isCollapsed ? 'justify-center' : ''}
                ${isActive 
                  ? 'bg-fire-100 dark:bg-fire-900/30 text-fire-700 dark:text-fire-400' 
                  : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
                }
              `}
              title={isCollapsed ? calc.label : ''}
            >
              <span className="text-xl">{calc.icon}</span>
              {!isCollapsed && <span>{calc.label}</span>}
            </NavLink>
          ))}
        </div>
      </nav>

      {/* Footer */}
      <div className="p-4 border-t border-gray-200 dark:border-gray-800">
        {/* Theme toggle */}
        {!isCollapsed ? (
          <div className="flex items-center justify-between mb-4">
            <span className="text-sm text-gray-600 dark:text-gray-400">Theme</span>
            <div className="flex items-center bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
              <button
                onClick={() => setTheme('light')}
                className={`p-1.5 rounded-md transition-colors ${
                  theme === 'light' 
                    ? 'bg-white dark:bg-gray-700 shadow-sm' 
                    : 'hover:bg-gray-200 dark:hover:bg-gray-700'
                }`}
                aria-label="Light mode"
              >
                <svg className="w-4 h-4 text-yellow-500" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 2a1 1 0 011 1v1a1 1 0 11-2 0V3a1 1 0 011-1zm4 8a4 4 0 11-8 0 4 4 0 018 0zm-.464 4.95l.707.707a1 1 0 001.414-1.414l-.707-.707a1 1 0 00-1.414 1.414zm2.12-10.607a1 1 0 010 1.414l-.706.707a1 1 0 11-1.414-1.414l.707-.707a1 1 0 011.414 0zM17 11a1 1 0 100-2h-1a1 1 0 100 2h1zm-7 4a1 1 0 011 1v1a1 1 0 11-2 0v-1a1 1 0 011-1zM5.05 6.464A1 1 0 106.465 5.05l-.708-.707a1 1 0 00-1.414 1.414l.707.707zm1.414 8.486l-.707.707a1 1 0 01-1.414-1.414l.707-.707a1 1 0 011.414 1.414zM4 11a1 1 0 100-2H3a1 1 0 000 2h1z" clipRule="evenodd" />
                </svg>
              </button>
              <button
                onClick={() => setTheme('dark')}
                className={`p-1.5 rounded-md transition-colors ${
                  theme === 'dark' 
                    ? 'bg-white dark:bg-gray-700 shadow-sm' 
                    : 'hover:bg-gray-200 dark:hover:bg-gray-700'
                }`}
                aria-label="Dark mode"
              >
                <svg className="w-4 h-4 text-blue-500" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z" />
                </svg>
              </button>
              <button
                onClick={() => setTheme('system')}
                className={`p-1.5 rounded-md transition-colors ${
                  theme === 'system' 
                    ? 'bg-white dark:bg-gray-700 shadow-sm' 
                    : 'hover:bg-gray-200 dark:hover:bg-gray-700'
                }`}
                aria-label="System theme"
              >
                <svg className="w-4 h-4 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
              </button>
            </div>
          </div>
        ) : (
          <div className="flex flex-col items-center gap-2 mb-4">
            <button
              onClick={() => setTheme('light')}
              className={`p-2 rounded-md transition-colors ${
                theme === 'light' 
                  ? 'bg-fire-100 dark:bg-fire-900/30 text-fire-700 dark:text-fire-400' 
                  : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-600 dark:text-gray-400'
              }`}
              aria-label="Light mode"
              title="Light mode"
            >
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 2a1 1 0 011 1v1a1 1 0 11-2 0V3a1 1 0 011-1zm4 8a4 4 0 11-8 0 4 4 0 018 0zm-.464 4.95l.707.707a1 1 0 001.414-1.414l-.707-.707a1 1 0 00-1.414 1.414zm2.12-10.607a1 1 0 010 1.414l-.706.707a1 1 0 11-1.414-1.414l.707-.707a1 1 0 011.414 0zM17 11a1 1 0 100-2h-1a1 1 0 100 2h1zm-7 4a1 1 0 011 1v1a1 1 0 11-2 0v-1a1 1 0 011-1zM5.05 6.464A1 1 0 106.465 5.05l-.708-.707a1 1 0 00-1.414 1.414l.707.707zm1.414 8.486l-.707.707a1 1 0 01-1.414-1.414l.707-.707a1 1 0 011.414 1.414zM4 11a1 1 0 100-2H3a1 1 0 000 2h1z" clipRule="evenodd" />
              </svg>
            </button>
            <button
              onClick={() => setTheme('dark')}
              className={`p-2 rounded-md transition-colors ${
                theme === 'dark' 
                  ? 'bg-fire-100 dark:bg-fire-900/30 text-fire-700 dark:text-fire-400' 
                  : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-600 dark:text-gray-400'
              }`}
              aria-label="Dark mode"
              title="Dark mode"
            >
              <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z" />
              </svg>
            </button>
            <button
              onClick={() => setTheme('system')}
              className={`p-2 rounded-md transition-colors ${
                theme === 'system' 
                  ? 'bg-fire-100 dark:bg-fire-900/30 text-fire-700 dark:text-fire-400' 
                  : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-600 dark:text-gray-400'
              }`}
              aria-label="System theme"
              title="System theme"
            >
              <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
            </button>
          </div>
        )}

        {/* Privacy badge */}
        {!isCollapsed ? (
          <div className="flex items-center gap-2 px-3 py-2 bg-green-50 dark:bg-green-900/20 rounded-lg">
            <svg className="w-4 h-4 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
            <span className="text-xs font-medium text-green-700 dark:text-green-300">100% Private & Offline</span>
          </div>
        ) : (
          <div className="flex justify-center p-2" title="100% Private & Offline">
            <svg className="w-5 h-5 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
          </div>
        )}
      </div>
    </aside>
  )
}
