/** All API endpoint base paths — routed via API Gateway at VITE_API_BASE_URL */
export const ENDPOINTS = {
  // Auth service
  AUTH_SEED: '/api/auth/seed',
  AUTH_USERS: '/api/auth/users',
  AUTH_CHANGE_PASSWORD: '/api/auth/change-password',

  // Patient service
  PATIENTS: '/api/patients',

  // Doctor service
  DOCTORS: '/api/doctors',
  DOCTOR_SCHEDULES: '/api/doctor-schedules',

  // Appointment service
  APPOINTMENTS: '/api/appointments',

  // Medical records service
  MEDICAL_RECORDS: '/api/medical-records',

  // Pharmacy service
  DRUGS: '/api/drugs',
  PRESCRIPTIONS: '/api/prescriptions',

  // Billing / Payment service
  PAYMENTS: '/api/payments',
  BILLING: '/api/billing',

  // Dashboard service
  DASHBOARD: '/api/dashboard',

  // Notification service
  NOTIFICATIONS: '/api/notifications',

  // File service
  FILES: '/api/files',
} as const;
