import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useCreatePatient } from './use-patients';

type FormValues = {
  firstName: string;
  lastName: string;
  email: string;
  dateOfBirth: string;
  phoneNumber: string;
  password: string;
  confirmPassword: string;
};

/** Full-page patient registration form — used by receptionist/admin to onboard new patients. */
export default function PatientRegisterPage() {
  const navigate = useNavigate();
  const createMutation = useCreatePatient();

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>();

  const onSubmit = async (values: FormValues) => {
    await createMutation.mutateAsync({
      email: values.email,
      firstName: values.firstName,
      lastName: values.lastName,
      dateOfBirth: values.dateOfBirth,
      phoneNumber: values.phoneNumber || undefined,
      password: values.password,
    });
    navigate('/patients');
  };

  const serverError = (createMutation.error as any)?.response?.data?.error;

  return (
    <div className="max-w-xl mx-auto">
      {/* Breadcrumb */}
      <nav className="text-sm text-gray-500 mb-4">
        <button onClick={() => navigate('/patients')} className="hover:underline">
          Patients
        </button>
        {' / Register'}
      </nav>

      <h1 className="text-2xl font-bold mb-6">Register New Patient</h1>

      <div className="bg-white border border-gray-200 rounded-lg p-6 shadow-sm">
        {serverError && (
          <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded px-4 py-3">
            {serverError}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          {/* Name row */}
          <div className="grid grid-cols-2 gap-4">
            <Field label="First Name" error={errors.firstName?.message}>
              <input
                {...register('firstName', { required: 'Required' })}
                className={inputCls}
                placeholder="Nguyen"
              />
            </Field>
            <Field label="Last Name" error={errors.lastName?.message}>
              <input
                {...register('lastName', { required: 'Required' })}
                className={inputCls}
                placeholder="Van A"
              />
            </Field>
          </div>

          <Field label="Email" error={errors.email?.message}>
            <input
              type="email"
              {...register('email', {
                required: 'Required',
                pattern: { value: /^\S+@\S+\.\S+$/, message: 'Invalid email' },
              })}
              className={inputCls}
              placeholder="patient@example.com"
            />
          </Field>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Date of Birth" error={errors.dateOfBirth?.message}>
              <input
                type="date"
                {...register('dateOfBirth', { required: 'Required' })}
                className={inputCls}
              />
            </Field>
            <Field label="Phone Number">
              <input
                {...register('phoneNumber')}
                className={inputCls}
                placeholder="0901234567"
              />
            </Field>
          </div>

          <hr className="my-1" />

          {/* Account credentials */}
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">
            Account Credentials
          </p>

          <Field label="Password" error={errors.password?.message}>
            <input
              type="password"
              {...register('password', {
                required: 'Required',
                minLength: { value: 8, message: 'Min 8 characters' },
              })}
              className={inputCls}
              placeholder="Min 8 characters"
            />
          </Field>

          <Field label="Confirm Password" error={errors.confirmPassword?.message}>
            <input
              type="password"
              {...register('confirmPassword', {
                required: 'Required',
                validate: v => v === watch('password') || 'Passwords do not match',
              })}
              className={inputCls}
            />
          </Field>

          {/* Actions */}
          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={() => navigate('/patients')}
              className="px-4 py-2 text-sm border border-gray-300 rounded hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting || createMutation.isPending}
              className="px-6 py-2 text-sm bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50"
            >
              {createMutation.isPending ? 'Registering...' : 'Register Patient'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

const inputCls =
  'w-full border border-gray-300 rounded px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400';

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="block text-xs font-medium text-gray-700 mb-1">{label}</label>
      {children}
      {error && <p className="text-xs text-red-500 mt-1">{error}</p>}
    </div>
  );
}
