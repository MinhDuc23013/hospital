// Unit tests for ScheduleAppointmentSchema and CancelAppointmentSchema.
import { describe, it, expect } from 'vitest'
import { ScheduleAppointmentSchema, CancelAppointmentSchema } from '@/lib/validators/appointment-schema'

describe('ScheduleAppointmentSchema', () => {
  const validInput = {
    providerId: 'provider-abc',
    scheduledTime: '2026-03-19T14:30:00Z',
    duration: 'PT30M',
  }

  it('passes with valid input', () => {
    const result = ScheduleAppointmentSchema.safeParse(validInput)
    expect(result.success).toBe(true)
  })

  it('fails when providerId is empty', () => {
    const result = ScheduleAppointmentSchema.safeParse({ ...validInput, providerId: '' })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Provider is required')
    }
  })

  it('fails when scheduledTime is not a valid ISO datetime', () => {
    const result = ScheduleAppointmentSchema.safeParse({ ...validInput, scheduledTime: 'not-a-date' })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Valid date and time required')
    }
  })

  it('fails when notes exceed 500 characters', () => {
    const result = ScheduleAppointmentSchema.safeParse({ ...validInput, notes: 'x'.repeat(501) })
    expect(result.success).toBe(false)
  })

  it('passes without optional notes', () => {
    const result = ScheduleAppointmentSchema.safeParse(validInput)
    expect(result.success).toBe(true)
  })

  it('defaults duration to PT30M when not provided', () => {
    const { duration, ...withoutDuration } = validInput
    const result = ScheduleAppointmentSchema.safeParse(withoutDuration)
    expect(result.success).toBe(true)
    if (result.success) {
      expect(result.data.duration).toBe('PT30M')
    }
  })
})

describe('CancelAppointmentSchema', () => {
  it('passes with valid UUID and optional reason', () => {
    const result = CancelAppointmentSchema.safeParse({
      appointmentId: '123e4567-e89b-12d3-a456-426614174000',
    })
    expect(result.success).toBe(true)
  })

  it('fails when appointmentId is not a UUID', () => {
    const result = CancelAppointmentSchema.safeParse({ appointmentId: 'not-a-uuid' })
    expect(result.success).toBe(false)
  })

  it('fails when reason exceeds 200 characters', () => {
    const result = CancelAppointmentSchema.safeParse({
      appointmentId: '123e4567-e89b-12d3-a456-426614174000',
      reason: 'x'.repeat(201),
    })
    expect(result.success).toBe(false)
  })
})
