import { DataTypes, Model, Optional } from 'sequelize';
import { sequelize } from '../database/sequelize';

type PrescriptionStatus = 'Pending' | 'Dispensed' | 'Expired';

interface PrescriptionAttributes {
  id: string;
  patientId: string;
  drugId: number;
  quantity: number;
  instructions?: string;
  status: PrescriptionStatus;
  issuedAt: Date;
  validUntil?: Date;
}

interface PrescriptionCreationAttributes extends Optional<PrescriptionAttributes, 'id' | 'status' | 'issuedAt'> {}

export class Prescription extends Model<PrescriptionAttributes, PrescriptionCreationAttributes>
  implements PrescriptionAttributes {
  declare id: string;
  declare patientId: string;
  declare drugId: number;
  declare quantity: number;
  declare instructions?: string;
  declare status: PrescriptionStatus;
  declare issuedAt: Date;
  declare validUntil?: Date;
}

Prescription.init({
  id: { type: DataTypes.UUID, primaryKey: true, defaultValue: DataTypes.UUIDV4 },
  patientId: { type: DataTypes.UUID, allowNull: false },
  drugId: { type: DataTypes.INTEGER, allowNull: false },
  quantity: { type: DataTypes.INTEGER, allowNull: false },
  instructions: DataTypes.TEXT,
  status: { type: DataTypes.ENUM('Pending', 'Dispensed', 'Expired'), defaultValue: 'Pending' },
  issuedAt: { type: DataTypes.DATE, defaultValue: DataTypes.NOW },
  validUntil: DataTypes.DATE,
}, {
  sequelize,
  tableName: 'prescriptions',
  timestamps: true,
  underscored: true,
});
