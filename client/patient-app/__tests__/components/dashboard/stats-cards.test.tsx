// Component tests for StatsCards dashboard widget.
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { StatsCards } from '@/components/dashboard/stats-cards'

describe('StatsCards', () => {
  it('renders all three stat cards', () => {
    render(
      <StatsCards upcomingCount={3} totalRecords={12} activePrescriptions={2} />
    )
    expect(screen.getByText('Upcoming Appointments')).toBeInTheDocument()
    expect(screen.getByText('Medical Records')).toBeInTheDocument()
    expect(screen.getByText('Active Prescriptions')).toBeInTheDocument()
  })

  it('displays the correct counts', () => {
    render(
      <StatsCards upcomingCount={5} totalRecords={8} activePrescriptions={3} />
    )
    expect(screen.getByText('5')).toBeInTheDocument()
    expect(screen.getByText('8')).toBeInTheDocument()
    expect(screen.getByText('3')).toBeInTheDocument()
  })

  it('renders zero values without crashing', () => {
    render(
      <StatsCards upcomingCount={0} totalRecords={0} activePrescriptions={0} />
    )
    // All three zeros — getAllByText to handle duplicates
    const zeros = screen.getAllByText('0')
    expect(zeros).toHaveLength(3)
  })
})
