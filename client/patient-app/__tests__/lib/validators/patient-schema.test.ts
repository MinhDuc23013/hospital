// Unit tests for UpdatePatientSchema.
import { describe, it, expect } from 'vitest'
import { UpdatePatientSchema } from '@/lib/validators/patient-schema'

describe('UpdatePatientSchema', () => {
  const validInput = {
    firstName: 'Jane',
    lastName: 'Doe',
  }

  it('passes with valid first and last name', () => {
    const result = UpdatePatientSchema.safeParse(validInput)
    expect(result.success).toBe(true)
  })

  it('fails when firstName is empty', () => {
    const result = UpdatePatientSchema.safeParse({ ...validInput, firstName: '' })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('First name is required')
    }
  })

  it('fails when firstName exceeds 100 characters', () => {
    const result = UpdatePatientSchema.safeParse({ ...validInput, firstName: 'A'.repeat(101) })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('First name must be 100 characters or less')
    }
  })

  it('fails when lastName is empty', () => {
    const result = UpdatePatientSchema.safeParse({ ...validInput, lastName: '' })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Last name is required')
    }
  })

  it('passes with optional phoneNumber in valid format', () => {
    const result = UpdatePatientSchema.safeParse({ ...validInput, phoneNumber: '+1 555-123-4567' })
    expect(result.success).toBe(true)
  })

  it('fails when phoneNumber has invalid format', () => {
    const result = UpdatePatientSchema.safeParse({ ...validInput, phoneNumber: 'abc' })
    expect(result.success).toBe(false)
  })

  it('passes when phoneNumber is empty string', () => {
    const result = UpdatePatientSchema.safeParse({ ...validInput, phoneNumber: '' })
    expect(result.success).toBe(true)
  })

  it('passes with optional address', () => {
    const result = UpdatePatientSchema.safeParse({ ...validInput, address: '123 Main St' })
    expect(result.success).toBe(true)
  })

  it('fails when address exceeds 255 characters', () => {
    const result = UpdatePatientSchema.safeParse({ ...validInput, address: 'x'.repeat(256) })
    expect(result.success).toBe(false)
  })
})
