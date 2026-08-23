import { Flame, Menu } from 'lucide-react'
import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import SiteFooter from './SiteFooter'
import { useState, useEffect } from 'react'
import UpdatePrompt from '../UpdatePrompt'

export default function AppLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(() => {
    const saved = localStorage.getItem('sidebarCollapsed')
    return saved === 'true'
  })

  useEffect(() => {
    localStorage.setItem('sidebarCollapsed', String(sidebarCollapsed))
  }, [sidebarCollapsed])

  const toggleSidebarCollapse = () => {
    setSidebarCollapsed(!sidebarCollapsed)
  }

  return (
    <>
      <UpdatePrompt />
      <div className="min-h-screen flex bg-surface">
      {/* Mobile menu overlay */}
      {sidebarOpen && (
        <div 
          className="fixed inset-0 z-40 bg-black/50 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}
      
      {/* Sidebar */}
      <Sidebar 
        isOpen={sidebarOpen} 
        onClose={() => setSidebarOpen(false)}
        isCollapsed={sidebarCollapsed}
        onToggleCollapse={toggleSidebarCollapse}
      />

      {/* Main content */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Mobile header */}
        <header className="lg:hidden sticky top-0 z-30 flex h-16 items-center gap-3 border-b border-border-subtle bg-surface-raised px-4">
          <button
            onClick={() => setSidebarOpen(true)}
            className="-ml-2 rounded-control p-2 text-content-muted transition-colors hover:bg-surface-sunken hover:text-content motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label="Open menu"
            aria-expanded={sidebarOpen}
            aria-controls="sidebar-navigation"
          >
            <Menu className="h-6 w-6" aria-hidden="true" strokeWidth={1.5} />
          </button>
          <div className="flex items-center gap-2">
            <Flame className="h-5 w-5 text-accent" aria-hidden="true" strokeWidth={1.5} />
            <span className="text-sm font-semibold text-content">FIRE Calculators</span>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-auto">
          <div className="max-w-6xl mx-auto p-4 sm:p-6 lg:p-8">
            <Outlet />
            <SiteFooter />
          </div>
        </main>
      </div>
      </div>
    </>
  )
}
