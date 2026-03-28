// Component tests for StatusBadge.
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { StatusBadge } from '@/components/shared/status-badge'

describe('StatusBadge', () => {
  it('renders with the correct formatted text for "Scheduled"', () => {
    render(<StatusBadge status="Scheduled" />)
    expect(screen.getByText('Scheduled')).toBeInTheDocument()
  })

  it('renders "InProgress" formatted as "In Progress"', () => {
    render(<StatusBadge status="InProgress" />)
    expect(screen.getByText('In Progress')).toBeInTheDocument()
  })

  it('renders "Cancelled" with correct text', () => {
    render(<StatusBadge status="Cancelled" />)
    expect(screen.getByText('Cancelled')).toBeInTheDocument()
  })

  it('renders an unknown status without crashing', () => {
    render(<StatusBadge status="UnknownStatus" />)
    expect(screen.getByText('Unknown Status')).toBeInTheDocument()
  })

  it('applies an additional className when provided', () => {
    const { container } = render(<StatusBadge status="Completed" className="extra-class" />)
    expect(container.firstChild).toHaveClass('extra-class')
  })
})
