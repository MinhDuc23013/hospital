import { useForm } from 'react-hook-form';
import { useCreateSchedule, useDoctors } from './use-doctors';
import type { CreateSchedulePayload } from '../../shared/types/doctor';

interface Props {
  onClose: () => void;
}

export function CreateScheduleModal({ onClose }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<CreateSchedulePayload>();
  const createMutation = useCreateSchedule();
  const { data: doctorsData } = useDoctors(undefined, true, 1, 100);

  const onSubmit = async (values: CreateSchedulePayload) => {
    await createMutation.mutateAsync({
      ...values,
      slotDurationMinutes: Number(values.slotDurationMinutes),
    });
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-lg font-semibold mb-4">Create Schedule</h2>

        {createMutation.error && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
            {(createMutation.error as Error).message || 'An error occurred'}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Doctor</label>
            <select {...register('doctorId', { required: 'Required' })}
              className="w-full border rounded px-3 py-2 text-sm">
              <option value="">Select doctor...</option>
              {doctorsData?.data.map(d => (
                <option key={d.id} value={d.id}>{d.fullName} — {d.specialty}</option>
              ))}
            </select>
            {errors.doctorId && <p className="text-red-500 text-xs mt-1">{errors.doctorId.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Date</label>
            <input {...register('date', { required: 'Required' })} type="date"
              className="w-full border rounded px-3 py-2 text-sm" />
            {errors.date && <p className="text-red-500 text-xs mt-1">{errors.date.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">Start Time</label>
              <input {...register('startTime', { required: 'Required' })} type="time"
                className="w-full border rounded px-3 py-2 text-sm" />
              {errors.startTime && <p className="text-red-500 text-xs mt-1">{errors.startTime.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">End Time</label>
              <input {...register('endTime', { required: 'Required' })} type="time"
                className="w-full border rounded px-3 py-2 text-sm" />
              {errors.endTime && <p className="text-red-500 text-xs mt-1">{errors.endTime.message}</p>}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Slot Duration (minutes)</label>
            <input {...register('slotDurationMinutes', { required: 'Required', min: 5 })}
              type="number" min={5} defaultValue={30}
              className="w-full border rounded px-3 py-2 text-sm" />
            {errors.slotDurationMinutes && <p className="text-red-500 text-xs mt-1">{errors.slotDurationMinutes.message}</p>}
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose}
              className="px-4 py-2 text-sm border rounded hover:bg-gray-50">Cancel</button>
            <button type="submit" disabled={createMutation.isPending}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
              {createMutation.isPending ? 'Creating...' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
