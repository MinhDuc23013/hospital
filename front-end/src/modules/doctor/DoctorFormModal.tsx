import { useForm } from 'react-hook-form';
import { useCreateDoctor, useUpdateDoctor } from './use-doctors';
import type { Doctor, CreateDoctorPayload, UpdateDoctorPayload } from '../../shared/types/doctor';

interface Props {
  mode: 'create' | 'edit';
  doctor?: Doctor;
  onClose: () => void;
}

type FormValues = CreateDoctorPayload;

export function DoctorFormModal({ mode, doctor, onClose }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    defaultValues: mode === 'edit' && doctor
      ? { fullName: doctor.fullName, specialty: doctor.specialty, email: doctor.email, phone: doctor.phone, password: '' }
      : undefined,
  });

  const createMutation = useCreateDoctor();
  const updateMutation = useUpdateDoctor(doctor?.id ?? '');
  const isPending = createMutation.isPending || updateMutation.isPending;
  const error = createMutation.error || updateMutation.error;

  const onSubmit = async (values: FormValues) => {
    if (mode === 'create') {
      await createMutation.mutateAsync(values);
    } else {
      const payload: UpdateDoctorPayload = {
        fullName: values.fullName,
        specialty: values.specialty,
        email: values.email,
        phone: values.phone,
      };
      await updateMutation.mutateAsync(payload);
    }
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-lg font-semibold mb-4">{mode === 'create' ? 'Add Doctor' : 'Edit Doctor'}</h2>

        {error && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
            {(error as Error).message || 'An error occurred'}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Full Name</label>
            <input {...register('fullName', { required: 'Required' })}
              className="w-full border rounded px-3 py-2 text-sm" />
            {errors.fullName && <p className="text-red-500 text-xs mt-1">{errors.fullName.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Specialty</label>
            <input {...register('specialty', { required: 'Required' })}
              className="w-full border rounded px-3 py-2 text-sm" />
            {errors.specialty && <p className="text-red-500 text-xs mt-1">{errors.specialty.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Email</label>
            <input {...register('email')} type="email" className="w-full border rounded px-3 py-2 text-sm" />
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Phone</label>
            <input {...register('phone')} className="w-full border rounded px-3 py-2 text-sm" />
          </div>

          {mode === 'create' && (
            <div>
              <label className="block text-sm font-medium mb-1">Password</label>
              <input {...register('password', { required: 'Required' })}
                type="password" className="w-full border rounded px-3 py-2 text-sm" />
              {errors.password && <p className="text-red-500 text-xs mt-1">{errors.password.message}</p>}
            </div>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose}
              className="px-4 py-2 text-sm border rounded hover:bg-gray-50">
              Cancel
            </button>
            <button type="submit" disabled={isPending}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
              {isPending ? 'Saving...' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
