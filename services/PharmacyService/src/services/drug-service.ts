import { Op, col } from 'sequelize';
import { Drug } from '../models/drug';
import { createLogger } from '@hospital/shared';

const logger = createLogger('pharmacy-service');

export class DrugService {
  async list(page = 1, pageSize = 50, search?: string): Promise<{ items: Drug[]; total: number }> {
    const where = search ? { name: { [Op.like]: `%${search}%` } } : {};
    const { rows: items, count: total } = await Drug.findAndCountAll({
      where,
      offset: (page - 1) * pageSize,
      limit: pageSize,
      order: [['name', 'ASC']],
    });
    return { items, total };
  }

  async getById(id: number): Promise<Drug | null> {
    return Drug.findByPk(id);
  }

  async getLowStock(): Promise<Drug[]> {
    return Drug.findAll({
      where: { currentStock: { [Op.lt]: col('minimumStock') } },
    });
  }
}
