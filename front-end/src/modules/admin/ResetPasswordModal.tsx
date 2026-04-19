import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useResetPassword } from './use-admin-users';
import type { KeycloakUser } from '../../shared/types/admin';

interface Props {
  user: KeycloakUser;
  onClose: () => void;
}

interface FormValues {
  newPassword: string;
  confirmPassword: string;
}

/** Compute a naive password strength score 0-4 */
function computeStrength(pw: string): { score: number; label: string; color: string } {
  if (!pw) return { score: 0, label: '', color: 'bg-gray-200' };
  let score = 0;
  if (pw.length >= 8) score++;
  if (/[A-Z]/.test(pw) && /[a-z]/.test(pw)) score++;
  if (/\d/.test(pw)) score++;
  if (/[^A-Za-z0-9]/.test(pw)) score++;

  const labels = ['Very weak', 'Weak', 'Fair', 'Good', 'Strong'];
  const colors = ['bg-red-500', 'bg-orange-500', 'bg-yellow-500', 'bg-blue-500', 'bg-green-500'];
  return { score, label: labels[score], color: colors[score] };
}

export function ResetPasswordModal({ user, onClose }: Props) {
  const [showPassword, setShowPassword] = useState(false);
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<FormValues>();

  const mutation = useResetPassword();
  const newPassword = watch('newPassword', '');
  const strength = computeStrength(newPassword);

  const onSubmit = async (values: FormValues) => {
    await mutation.mutateAsync({
      userId: user.id,
      newPassword: values.newPassword,
    });
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h2 className="text-lg font-semibold">Reset Password</h2>
            <p className="text-xs text-gray-500 mt-1">
              For user: <span className="font-medium text-gray-700">{user.email}</span>
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-xl leading-none"
            aria-label="Close"
          >
            ×
          </button>
        </div>

        {mutation.error && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
            {(mutation.error as Error).message || 'Failed to reset password'}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">New Password</label>
            <div className="relative">
              <input
                {...register('newPassword', {
                  required: 'Password is required',
                  minLength: { value: 8, message: 'Minimum 8 characters' },
                })}
                type={showPassword ? 'text' : 'password'}
                className="w-full border rounded px-3 py-2 text-sm pr-20"
                placeholder="Enter new password"
                autoComplete="new-password"
              />
              <button
                type="button"
                onClick={() => setShowPassword(s => !s)}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-xs text-blue-600 hover:text-blue-800 px-2"
              >
                {showPassword ? 'Hide' : 'Show'}
              </button>
            </div>
            {errors.newPassword && (
              <p className="text-red-500 text-xs mt-1">{errors.newPassword.message}</p>
            )}

            {/* Strength indicator */}
            {newPassword && (
              <div className="mt-2">
                <div className="flex gap-1 h-1">
                  {[0, 1, 2, 3].map(i => (
                    <div
                      key={i}
                      className={`flex-1 rounded ${
                        i < strength.score ? strength.color : 'bg-gray-200'
                      }`}
                    />
                  ))}
                </div>
                <p className="text-xs text-gray-500 mt-1">
                  Strength: <span className="font-medium">{strength.label}</span>
                </p>
              </div>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Confirm Password</label>
            <input
              {...register('confirmPassword', {
                required: 'Please confirm password',
                validate: value => value === newPassword || 'Passwords do not match',
              })}
              type={showPassword ? 'text' : 'password'}
              className="w-full border rounded px-3 py-2 text-sm"
              placeholder="Re-enter new password"
              autoComplete="new-password"
            />
            {errors.confirmPassword && (
              <p className="text-red-500 text-xs mt-1">{errors.confirmPassword.message}</p>
            )}
          </div>

          <div className="bg-yellow-50 border border-yellow-200 rounded p-3 text-xs text-yellow-800">
            <strong>Note:</strong> User will need to sign in again with the new password.
            This action cannot be undone.
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
              disabled={mutation.isPending}
              className="px-4 py-2 text-sm bg-yellow-600 text-white rounded hover:bg-yellow-700 disabled:opacity-50"
            >
              {mutation.isPending ? 'Resetting...' : 'Reset Password'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
