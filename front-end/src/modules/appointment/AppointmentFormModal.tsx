import { useForm } from 'react-hook-form';
import { useBookAppointment } from './use-appointments';
import type { BookAppointmentPayload } from '../../shared/types/appointment';

interface Props {
  onClose: () => void;
}

export function AppointmentFormModal({ onClose }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<BookAppointmentPayload>({
    defaultValues: { durationMinutes: 30, paymentMethod: 'Cash' },
  });
  const bookMutation = useBookAppointment();

  const onSubmit = async (values: BookAppointmentPayload) => {
    await bookMutation.mutateAsync({
      ...values,
      durationMinutes: Number(values.durationMinutes),
    });
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-lg font-semibold mb-4">Create Appointment</h2>

        {bookMutation.error && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
            {(bookMutation.error as Error).message || 'An error occurred'}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Patient ID</label>
            <input {...register('patientId', { required: 'Required' })}
              className="w-full border rounded px-3 py-2 text-sm" placeholder="Patient UUID" />
            {errors.patientId && <p className="text-red-500 text-xs mt-1">{errors.patientId.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Doctor ID</label>
            <input {...register('doctorId', { required: 'Required' })}
              className="w-full border rounded px-3 py-2 text-sm" placeholder="Doctor UUID" />
            {errors.doctorId && <p className="text-red-500 text-xs mt-1">{errors.doctorId.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Scheduled Time</label>
            <input {...register('scheduledTime', { required: 'Required' })} type="datetime-local"
              className="w-full border rounded px-3 py-2 text-sm" />
            {errors.scheduledTime && <p className="text-red-500 text-xs mt-1">{errors.scheduledTime.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Duration (minutes)</label>
            <input {...register('durationMinutes', { required: 'Required', min: 5 })}
              type="number" min={5} className="w-full border rounded px-3 py-2 text-sm" />
            {errors.durationMinutes && <p className="text-red-500 text-xs mt-1">{errors.durationMinutes.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Payment Method</label>
            <select {...register('paymentMethod', { required: 'Required' })}
              className="w-full border rounded px-3 py-2 text-sm">
              <option value="Cash">Cash</option>
              <option value="Card">Card</option>
              <option value="Insurance">Insurance</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Notes</label>
            <textarea {...register('notes')} rows={2}
              className="w-full border rounded px-3 py-2 text-sm resize-none" />
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose}
              className="px-4 py-2 text-sm border rounded hover:bg-gray-50">Cancel</button>
            <button type="submit" disabled={bookMutation.isPending}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
              {bookMutation.isPending ? 'Booking...' : 'Book'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
