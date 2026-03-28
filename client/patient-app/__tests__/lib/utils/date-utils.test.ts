// Unit tests for date-utils — formatDate, formatTime, formatRelative, formatDuration.
import { describe, it, expect } from 'vitest'
import { formatDate, formatTime, formatDuration } from '@/lib/utils/date-utils'

describe('formatDate', () => {
  it('returns a readable long-form date string', () => {
    // Use a fixed UTC date; parseISO keeps it timezone-agnostic
    const result = formatDate('2026-03-19T00:00:00Z')
    // date-fns parseISO interprets as local time; assert it contains the month name
    expect(result).toMatch(/March|February/) // allow for UTC offset on CI
    expect(result).toMatch(/\d{4}/)
  })

  it('accepts a Date object', () => {
    const result = formatDate(new Date('2026-01-01T12:00:00Z'))
    expect(typeof result).toBe('string')
    expect(result.length).toBeGreaterThan(0)
  })
})

describe('formatTime', () => {
  it('returns a 12-hour time string', () => {
    const result = formatTime('2026-03-19T14:30:00Z')
    // Should contain AM or PM
    expect(result).toMatch(/AM|PM/)
  })
})

describe('formatDuration', () => {
  it('formats PT30M as "30 min"', () => {
    expect(formatDuration('PT30M')).toBe('30 min')
  })

  it('formats PT1H as "1 hr"', () => {
    expect(formatDuration('PT1H')).toBe('1 hr')
  })

  it('formats PT1H30M as "1 hr 30 min"', () => {
    expect(formatDuration('PT1H30M')).toBe('1 hr 30 min')
  })

  it('formats PT15M as "15 min"', () => {
    expect(formatDuration('PT15M')).toBe('15 min')
  })

  it('returns original string when no hours or minutes found', () => {
    expect(formatDuration('P1D')).toBe('P1D')
  })
})
