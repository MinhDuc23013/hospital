/** Event name constants matching HospitalShared.Constants.EventTypes (.NET) */
export const EventNames = {
  PatientCreated: 'hospital.patient.created',
  PatientUpdated: 'hospital.patient.updated',
  AppointmentScheduled: 'hospital.appointment.scheduled',
  AppointmentCancelled: 'hospital.appointment.cancelled',
  PrescriptionIssued: 'hospital.prescription.issued',
  InventoryLow: 'hospital.pharmacy.inventory_low',
  DrugDispensed: 'hospital.pharmacy.drug_dispensed',
  DrugBatchNearExpiry: 'hospital.pharmacy.batch_near_expiry',
  NotificationSent: 'hospital.notification.sent',
} as const;

export type EventName = typeof EventNames[keyof typeof EventNames];
