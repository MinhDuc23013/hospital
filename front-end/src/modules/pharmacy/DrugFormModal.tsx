import { useForm } from 'react-hook-form';
import { useCreateDrug, useUpdateDrug } from './use-pharmacy';
import type { Drug, CreateDrugPayload, UpdateDrugPayload } from '../../shared/types/pharmacy';

interface Props {
  mode: 'create' | 'edit';
  drug?: Drug;
  onClose: () => void;
}

type FormValues = CreateDrugPayload;

export function DrugFormModal({ mode, drug, onClose }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    defaultValues: mode === 'edit' && drug
      ? { name: drug.name, code: drug.code, dosage: drug.dosage, quantity: drug.quantity, price: drug.price, lowStockThreshold: drug.lowStockThreshold }
      : { lowStockThreshold: 10 },
  });

  const createMutation = useCreateDrug();
  const updateMutation = useUpdateDrug(drug?.id ?? '');
  const isPending = createMutation.isPending || updateMutation.isPending;
  const error = createMutation.error || updateMutation.error;

  const onSubmit = async (values: FormValues) => {
    const parsed = {
      ...values,
      quantity: Number(values.quantity),
      price: Number(values.price),
      lowStockThreshold: Number(values.lowStockThreshold),
    };
    if (mode === 'create') {
      await createMutation.mutateAsync(parsed);
    } else {
      const payload: UpdateDrugPayload = {
        name: parsed.name,
        dosage: parsed.dosage,
        quantity: parsed.quantity,
        price: parsed.price,
        lowStockThreshold: parsed.lowStockThreshold,
      };
      await updateMutation.mutateAsync(payload);
    }
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-lg font-semibold mb-4">{mode === 'create' ? 'Add Drug' : 'Edit Drug'}</h2>

        {error && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
            {(error as Error).message || 'An error occurred'}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Name</label>
            <input {...register('name', { required: 'Required' })}
              className="w-full border rounded px-3 py-2 text-sm" />
            {errors.name && <p className="text-red-500 text-xs mt-1">{errors.name.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Code</label>
            <input {...register('code', { required: 'Required' })}
              className="w-full border rounded px-3 py-2 text-sm"
              disabled={mode === 'edit'} />
            {errors.code && <p className="text-red-500 text-xs mt-1">{errors.code.message}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Dosage</label>
            <input {...register('dosage')} placeholder="e.g. 500mg"
              className="w-full border rounded px-3 py-2 text-sm" />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">Quantity</label>
              <input {...register('quantity', { required: 'Required', min: 0 })}
                type="number" min={0} className="w-full border rounded px-3 py-2 text-sm" />
              {errors.quantity && <p className="text-red-500 text-xs mt-1">{errors.quantity.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Price (VND)</label>
              <input {...register('price', { required: 'Required', min: 0 })}
                type="number" min={0} className="w-full border rounded px-3 py-2 text-sm" />
              {errors.price && <p className="text-red-500 text-xs mt-1">{errors.price.message}</p>}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Low Stock Threshold</label>
            <input {...register('lowStockThreshold', { min: 0 })}
              type="number" min={0} className="w-full border rounded px-3 py-2 text-sm" />
          </div>

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
