// Unit tests for format-utils — formatStatus, formatName.
import { describe, it, expect } from 'vitest'
import { formatStatus, formatName } from '@/lib/utils/format-utils'

describe('formatStatus', () => {
  it('converts "InProgress" to "In Progress"', () => {
    expect(formatStatus('InProgress')).toBe('In Progress')
  })

  it('leaves single-word status unchanged', () => {
    expect(formatStatus('Scheduled')).toBe('Scheduled')
    expect(formatStatus('Cancelled')).toBe('Cancelled')
    expect(formatStatus('Dispensed')).toBe('Dispensed')
  })

  it('handles multiple humps', () => {
    expect(formatStatus('SomeLongStatus')).toBe('Some Long Status')
  })

  it('returns empty string for empty input', () => {
    expect(formatStatus('')).toBe('')
  })
})

describe('formatName', () => {
  it('combines first and last name with a space', () => {
    expect(formatName('John', 'Doe')).toBe('John Doe')
  })

  it('trims extra whitespace when a part is empty', () => {
    expect(formatName('Jane', '')).toBe('Jane')
    expect(formatName('', 'Smith')).toBe('Smith')
  })
})
