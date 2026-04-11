import { useForm } from 'react-hook-form';
import { useCreateAdminUser, useUpdateAdminUser } from './use-admin-users';
import type { KeycloakUser, CreateUserPayload, UpdateUserPayload } from '../../shared/types/admin';

const ROLES = ['admin', 'doctor', 'nurse', 'pharmacist', 'receptionist', 'patient'];

interface Props {
  mode: 'create' | 'edit';
  user?: KeycloakUser;
  onClose: () => void;
}

export function UserFormModal({ mode, user, onClose }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<CreateUserPayload>({
    defaultValues: mode === 'edit' && user
      ? { firstName: user.firstName, lastName: user.lastName }
      : undefined,
  });

  const createMutation = useCreateAdminUser();
  const updateMutation = useUpdateAdminUser(user?.id ?? '');
  const isPending = createMutation.isPending || updateMutation.isPending;
  const error = createMutation.error || updateMutation.error;

  const onSubmit = async (values: CreateUserPayload) => {
    if (mode === 'create') {
      await createMutation.mutateAsync(values);
    } else {
      const payload: UpdateUserPayload = {
        firstName: values.firstName,
        lastName: values.lastName,
      };
      await updateMutation.mutateAsync(payload);
    }
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-lg font-semibold mb-4">{mode === 'create' ? 'Add User' : 'Edit User'}</h2>

        {error && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
            {(error as Error).message || 'An error occurred'}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">First Name</label>
            <input {...register('firstName', { required: 'Required' })}
              className="w-full border rounded px-3 py-2 text-sm" />
            {errors.firstName && <p className="text-red-500 text-xs mt-1">{errors.firstName.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Last Name</label>
            <input {...register('lastName', { required: 'Required' })}
              className="w-full border rounded px-3 py-2 text-sm" />
            {errors.lastName && <p className="text-red-500 text-xs mt-1">{errors.lastName.message}</p>}
          </div>

          {mode === 'create' && (
            <>
              <div>
                <label className="block text-sm font-medium mb-1">Email</label>
                <input {...register('email', { required: 'Required' })} type="email"
                  className="w-full border rounded px-3 py-2 text-sm" />
                {errors.email && <p className="text-red-500 text-xs mt-1">{errors.email.message}</p>}
              </div>

              <div>
                <label className="block text-sm font-medium mb-1">Password</label>
                <input {...register('password', { required: 'Required', minLength: { value: 6, message: 'Min 6 chars' } })}
                  type="password" className="w-full border rounded px-3 py-2 text-sm" />
                {errors.password && <p className="text-red-500 text-xs mt-1">{errors.password.message}</p>}
              </div>

              <div>
                <label className="block text-sm font-medium mb-1">Role</label>
                <select {...register('role', { required: 'Required' })}
                  className="w-full border rounded px-3 py-2 text-sm">
                  <option value="">Select role...</option>
                  {ROLES.map(r => <option key={r} value={r}>{r}</option>)}
                </select>
                {errors.role && <p className="text-red-500 text-xs mt-1">{errors.role.message}</p>}
              </div>
            </>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose}
              className="px-4 py-2 text-sm border rounded hover:bg-gray-50">Cancel</button>
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
