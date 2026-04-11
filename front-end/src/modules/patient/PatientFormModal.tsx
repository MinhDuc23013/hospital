import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useCreatePatient, useUpdatePatient } from './use-patients';
import type { Patient, CreatePatientPayload, UpdatePatientPayload } from '../../shared/types/patient';

interface Props {
  mode: 'create' | 'edit';
  patient?: Patient;
  onClose: () => void;
}

type FormValues = {
  email: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  phoneNumber: string;
  password: string;
};

export function PatientFormModal({ mode, patient, onClose }: Props) {
  const createMutation = useCreatePatient();
  const updateMutation = useUpdatePatient(patient?.id ?? '');

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>();

  useEffect(() => {
    if (mode === 'edit' && patient) {
      reset({
        firstName: patient.firstName,
        lastName: patient.lastName,
        phoneNumber: patient.phoneNumber ?? '',
      });
    }
  }, [mode, patient, reset]);

  const onSubmit = async (values: FormValues) => {
    if (mode === 'create') {
      const payload: CreatePatientPayload = {
        email: values.email,
        firstName: values.firstName,
        lastName: values.lastName,
        dateOfBirth: values.dateOfBirth,
        phoneNumber: values.phoneNumber || undefined,
        password: values.password,
      };
      await createMutation.mutateAsync(payload);
    } else {
      const payload: UpdatePatientPayload = {
        firstName: values.firstName,
        lastName: values.lastName,
        phoneNumber: values.phoneNumber || undefined,
      };
      await updateMutation.mutateAsync(payload);
    }
    onClose();
  };

  const isPending = createMutation.isPending || updateMutation.isPending;
  const serverError = createMutation.error || updateMutation.error;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-lg font-bold mb-4">
          {mode === 'create' ? 'Add Patient' : 'Edit Patient'}
        </h2>

        {serverError && (
          <p className="mb-3 text-sm text-red-600 bg-red-50 border border-red-200 rounded px-3 py-2">
            {(serverError as any)?.response?.data?.error ?? 'Something went wrong.'}
          </p>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <Field label="First Name" error={errors.firstName?.message}>
              <input {...register('firstName', { required: 'Required' })} className={inputCls} />
            </Field>
            <Field label="Last Name" error={errors.lastName?.message}>
              <input {...register('lastName', { required: 'Required' })} className={inputCls} />
            </Field>
          </div>

          {mode === 'create' && (
            <>
              <Field label="Email" error={errors.email?.message}>
                <input
                  type="email"
                  {...register('email', { required: 'Required' })}
                  className={inputCls}
                />
              </Field>
              <Field label="Date of Birth" error={errors.dateOfBirth?.message}>
                <input
                  type="date"
                  {...register('dateOfBirth', { required: 'Required' })}
                  className={inputCls}
                />
              </Field>
              <Field label="Password" error={errors.password?.message}>
                <input
                  type="password"
                  {...register('password', { required: 'Required', minLength: { value: 6, message: 'Min 6 chars' } })}
                  className={inputCls}
                />
              </Field>
            </>
          )}

          <Field label="Phone Number">
            <input {...register('phoneNumber')} className={inputCls} placeholder="Optional" />
          </Field>

          <div className="flex justify-end gap-2 pt-2">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm border rounded hover:bg-gray-50">
              Cancel
            </button>
            <button
              type="submit"
              disabled={isPending || isSubmitting}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
            >
              {isPending ? 'Saving...' : mode === 'create' ? 'Create' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

const inputCls = 'w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400';

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-xs font-medium text-gray-700 mb-1">{label}</label>
      {children}
      {error && <p className="text-xs text-red-500 mt-0.5">{error}</p>}
    </div>
  );
}
