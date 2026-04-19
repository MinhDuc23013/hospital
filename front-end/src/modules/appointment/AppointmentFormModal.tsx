import { useEffect, useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useBookAppointment } from './use-appointments';
import { appointmentApi } from './appointment-api';
import { doctorApi } from '../doctor/doctor-api';
import { patientApi } from '../patient/patient-api';
import { useAuth } from '../../auth/useAuth';
import type { BookAppointmentPayload, TimeSlot } from '../../shared/types/appointment';

interface Props {
  onClose: () => void;
}

const DEFAULT_PAYMENT_AMOUNT = 200_000;

export function AppointmentFormModal({ onClose }: Props) {
  const { hasRole, userId, fullName, email } = useAuth();
  const isPatient = hasRole('patient') && !hasRole('admin', 'doctor', 'receptionist');

  const bookMutation = useBookAppointment();

  const [patientId, setPatientId] = useState(isPatient && userId ? userId : '');
  const [doctorId, setDoctorId] = useState('');
  const [scheduleId, setScheduleId] = useState('');
  const [slotId, setSlotId] = useState('');
  const [paymentMethod, setPaymentMethod] = useState<'Cash' | 'Card' | 'Insurance' | 'BankTransfer'>('Cash');
  const [notes, setNotes] = useState('');
  const [formError, setFormError] = useState<string>();

  // Patient list (only for non-patient roles)
  const { data: patientsData } = useQuery({
    queryKey: ['patients', 'for-appointment'],
    queryFn: () => patientApi.list(1, 100),
    enabled: !isPatient,
  });

  // Doctor list
  const { data: doctorsData } = useQuery({
    queryKey: ['doctors', 'active'],
    queryFn: () => doctorApi.list(undefined, true, 1, 100),
  });

  // Schedules for selected doctor
  const { data: schedules, isLoading: schedulesLoading } = useQuery({
    queryKey: ['doctor-schedules', doctorId],
    queryFn: () => appointmentApi.listDoctorSchedules(doctorId),
    enabled: !!doctorId,
  });

  // Slots for selected schedule
  const { data: slots, isLoading: slotsLoading } = useQuery({
    queryKey: ['schedule-slots', scheduleId],
    queryFn: () => appointmentApi.listScheduleSlots(scheduleId),
    enabled: !!scheduleId,
  });

  // Reset dependent selects when parent changes
  useEffect(() => {
    setScheduleId('');
    setSlotId('');
  }, [doctorId]);
  useEffect(() => setSlotId(''), [scheduleId]);

  const selectedSchedule = useMemo(
    () => schedules?.find(s => s.id === scheduleId),
    [schedules, scheduleId],
  );
  const selectedSlot = useMemo(
    () => slots?.find(s => s.id === slotId),
    [slots, slotId],
  );

  const isSlotBooked = (s: TimeSlot) =>
    s.status !== 'Available' && s.status !== 0;

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(undefined);

    if (!patientId || !doctorId || !scheduleId || !slotId || !selectedSchedule || !selectedSlot) {
      setFormError('Please complete all required selections');
      return;
    }

    // Build scheduledTime from schedule date + slot startTime
    const baseDate = (selectedSchedule.date || '').split('T')[0];
    const scheduledTime = `${baseDate}T${selectedSlot.startTime}`;

    const payload: BookAppointmentPayload = {
      patientId,
      doctorId,
      scheduleId,
      slotId,
      scheduledTime,
      durationMinutes: selectedSchedule.slotDurationMinutes,
      paymentAmount: DEFAULT_PAYMENT_AMOUNT,
      paymentMethod,
      currency: 'VND',
      notes: notes || undefined,
    };

    try {
      await bookMutation.mutateAsync(payload);
      onClose();
    } catch (err: any) {
      setFormError(err?.response?.data?.failureReason || err?.message || 'Booking failed');
    }
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg p-6 max-h-[90vh] overflow-y-auto">
        <h2 className="text-lg font-semibold mb-4">Create Appointment</h2>

        {(formError || bookMutation.error) && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
            {formError || (bookMutation.error as Error)?.message || 'An error occurred'}
          </div>
        )}

        <form onSubmit={onSubmit} className="space-y-4">
          {/* Patient */}
          <div>
            <label className="block text-sm font-medium mb-1">Patient</label>
            {isPatient ? (
              <div className="px-3 py-2 bg-gray-50 border border-gray-200 rounded text-sm text-gray-700">
                {fullName || email} <span className="text-xs text-gray-400 ml-2">(you)</span>
              </div>
            ) : (
              <select
                value={patientId}
                onChange={e => setPatientId(e.target.value)}
                required
                className="w-full border rounded px-3 py-2 text-sm"
              >
                <option value="">Select patient...</option>
                {patientsData?.data?.map(p => (
                  <option key={p.id} value={p.id}>
                    {p.firstName} {p.lastName} — {p.email}
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Doctor */}
          <div>
            <label className="block text-sm font-medium mb-1">Doctor</label>
            <select
              value={doctorId}
              onChange={e => setDoctorId(e.target.value)}
              required
              className="w-full border rounded px-3 py-2 text-sm"
            >
              <option value="">Select doctor...</option>
              {doctorsData?.data?.map(d => (
                <option key={d.id} value={d.id}>
                  {d.fullName} — {d.specialty}
                </option>
              ))}
            </select>
          </div>

          {/* Schedule (date) */}
          <div>
            <label className="block text-sm font-medium mb-1">Schedule Date</label>
            <select
              value={scheduleId}
              onChange={e => setScheduleId(e.target.value)}
              required
              disabled={!doctorId || schedulesLoading}
              className="w-full border rounded px-3 py-2 text-sm disabled:bg-gray-100"
            >
              <option value="">
                {!doctorId ? 'Pick a doctor first' :
                  schedulesLoading ? 'Loading...' :
                  (schedules?.length === 0 ? 'No schedules available' : 'Select date...')}
              </option>
              {schedules?.map(s => (
                <option key={s.id} value={s.id}>
                  {new Date(s.date).toLocaleDateString()} · {s.startTime.slice(0, 5)}–{s.endTime.slice(0, 5)} · {s.availableSlots}/{s.totalSlots} available
                </option>
              ))}
            </select>
          </div>

          {/* Slots — grid */}
          <div>
            <label className="block text-sm font-medium mb-2">Time Slot</label>
            {!scheduleId ? (
              <div className="text-xs text-gray-400 bg-gray-50 border border-dashed border-gray-200 rounded p-4 text-center">
                Pick a date to view available time slots
              </div>
            ) : slotsLoading ? (
              <div className="text-xs text-gray-400 p-4">Loading slots...</div>
            ) : !slots || slots.length === 0 ? (
              <div className="text-xs text-gray-400 p-4">No slots</div>
            ) : (
              <div className="grid grid-cols-4 gap-2">
                {slots.map(s => {
                  const booked = isSlotBooked(s);
                  const isSelected = s.id === slotId;
                  return (
                    <button
                      key={s.id}
                      type="button"
                      disabled={booked}
                      onClick={() => setSlotId(s.id)}
                      className={`px-2 py-2 text-sm rounded border transition ${
                        booked
                          ? 'bg-gray-100 text-gray-400 border-gray-200 cursor-not-allowed line-through'
                          : isSelected
                            ? 'bg-blue-600 text-white border-blue-600'
                            : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400'
                      }`}
                      title={booked ? 'Already booked' : 'Available'}
                    >
                      {s.startTime.slice(0, 5)}
                    </button>
                  );
                })}
              </div>
            )}
          </div>

          {/* Payment */}
          <div>
            <label className="block text-sm font-medium mb-1">Payment Method</label>
            <select
              value={paymentMethod}
              onChange={e => setPaymentMethod(e.target.value as any)}
              className="w-full border rounded px-3 py-2 text-sm"
            >
              <option value="Cash">Cash</option>
              <option value="Card">Card</option>
              <option value="BankTransfer">Bank Transfer</option>
              <option value="Insurance">Insurance</option>
            </select>
          </div>

          {/* Notes */}
          <div>
            <label className="block text-sm font-medium mb-1">Notes (optional)</label>
            <textarea
              value={notes}
              onChange={e => setNotes(e.target.value)}
              rows={2}
              className="w-full border rounded px-3 py-2 text-sm resize-none"
              placeholder="Symptoms, special requirements..."
            />
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm border rounded hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={bookMutation.isPending || !slotId}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
            >
              {bookMutation.isPending ? 'Booking...' : 'Book Appointment'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
